using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace GCAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseGCKeepAliveAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GCA002";
        
        // Category for the analyzer
        private const string Category = "Reliability";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.UseGCKeepAliveTitle,
            messageFormat: Resources.UseGCKeepAliveMessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Resources.UseGCKeepAliveDescription,
            helpLinkUri: AnalyzerConstants.GetHelpLink(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for method analysis to find P/Invoke methods
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            
            // Check if method has DllImport attribute or calls other P/Invoke methods
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
            
            if (methodSymbol == null)
                return;
                
            // Check if method has P/Invoke calls
            var hasPInvoke = HasPInvokeCalls(methodDeclaration, context.SemanticModel);
            
            // If we have P/Invoke, check for potential objects that might need GC.KeepAlive
            if (hasPInvoke)
            {
                // Look for object parameters or local variables used in P/Invoke
                var parameters = methodDeclaration.ParameterList.Parameters;
                
                foreach (var parameter in parameters)
                {
                    if (parameter.Type == null)
                        continue;
                        
                    var parameterType = context.SemanticModel.GetTypeInfo(parameter.Type).Type;
                    
                    // Skip value types and string parameters
                    if (parameterType == null || parameterType.IsValueType || 
                        parameterType.SpecialType == SpecialType.System_String)
                        continue;
                        
                    // Check if the parameter is used in unsafe context or passed to a P/Invoke method
                    if (IsUsedInUnsafeContext(methodDeclaration, parameter.Identifier.ValueText, context.SemanticModel))
                    {
                        // Check if GC.KeepAlive is used for this parameter
                        if (!IsGCKeepAliveUsed(methodDeclaration, parameter.Identifier.ValueText))
                        {
                            var diagnostic = Diagnostic.Create(Rule, parameter.GetLocation(), parameter.Identifier.ValueText);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
        
        private bool HasPInvokeCalls(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            // Look for DllImport attribute
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeSymbol = semanticModel.GetSymbolInfo(attribute).Symbol;
                    if (attributeSymbol != null && 
                        attributeSymbol.ContainingType.ToDisplayString() == "System.Runtime.InteropServices.DllImportAttribute")
                    {
                        return true;
                    }
                }
            }
            
            // Check for calls to P/Invoke methods
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            
            foreach (var invocation in invocations)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (symbol != null && symbol.IsExtern)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private bool IsUsedInUnsafeContext(MethodDeclarationSyntax method, string parameterName, SemanticModel semanticModel)
        {
            // This is a simplified check - in a real analyzer, you'd need more thorough analysis
            // Look for unsafe blocks or fixed statements
            var unsafeBlocks = method.DescendantNodes().OfType<UnsafeStatementSyntax>();
            var fixedStatements = method.DescendantNodes().OfType<FixedStatementSyntax>();
            
            foreach (var unsafeBlock in unsafeBlocks)
            {
                var identifiers = unsafeBlock.DescendantNodes().OfType<IdentifierNameSyntax>();
                if (identifiers.Any(id => id.Identifier.ValueText == parameterName))
                {
                    return true;
                }
            }
            
            foreach (var fixedStatement in fixedStatements)
            {
                var identifiers = fixedStatement.DescendantNodes().OfType<IdentifierNameSyntax>();
                if (identifiers.Any(id => id.Identifier.ValueText == parameterName))
                {
                    return true;
                }
            }
            
            // Check if parameter is passed to P/Invoke methods
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            
            foreach (var invocation in invocations)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (symbol != null && symbol.IsExtern)
                {
                    var args = invocation.ArgumentList.Arguments;
                    foreach (var arg in args)
                    {
                        if (arg.Expression is IdentifierNameSyntax identifierName && 
                            identifierName.Identifier.ValueText == parameterName)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        private bool IsGCKeepAliveUsed(MethodDeclarationSyntax method, string parameterName)
        {
            // Look for GC.KeepAlive(parameterName) calls
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            
            foreach (var invocation in invocations)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.ValueText == "KeepAlive" &&
                    memberAccess.Expression is IdentifierNameSyntax identifierName &&
                    identifierName.Identifier.ValueText == "GC")
                {
                    var args = invocation.ArgumentList.Arguments;
                    foreach (var arg in args)
                    {
                        if (arg.Expression is IdentifierNameSyntax argIdentifier && 
                            argIdentifier.Identifier.ValueText == parameterName)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
    }
} 