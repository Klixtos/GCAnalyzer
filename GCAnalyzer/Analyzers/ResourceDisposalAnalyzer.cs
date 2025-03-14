using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace GCAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ResourceDisposalAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GCA003";
        
        // Category for the analyzer
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.ProperResourceDisposalTitle,
            messageFormat: Resources.ProperResourceDisposalMessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.ProperResourceDisposalDescription,
            helpLinkUri: AnalyzerConstants.GetHelpLink(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for method body analysis
            context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            
            // Look for local variable declarations
            var localDeclarations = methodDeclaration.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>();
                
            foreach (var localDeclaration in localDeclarations)
            {
                foreach (var variable in localDeclaration.Declaration.Variables)
                {
                    var variableSymbol = context.SemanticModel.GetDeclaredSymbol(variable);
                    if (variableSymbol == null)
                        continue;
                        
                    var variableType = context.SemanticModel.GetTypeInfo(localDeclaration.Declaration.Type).Type;
                    
                    // Check if the type implements IDisposable
                    if (variableType != null && ImplementsIDisposable(variableType))
                    {
                        // Check if the variable is disposed
                        var variableName = variable.Identifier.ValueText;
                        if (!IsDisposed(methodDeclaration, variableName, context.SemanticModel) && 
                            !IsReturnedOrPassedToMethod(methodDeclaration, variableName, context.SemanticModel))
                        {
                            var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), variableType.Name);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
        
        private bool ImplementsIDisposable(ITypeSymbol type)
        {
            // Check if this type or any of its interfaces is IDisposable
            if (type.Name == "IDisposable" && type.ContainingNamespace?.Name == "System")
                return true;
                
            return type.AllInterfaces.Any(i => i.Name == "IDisposable" && i.ContainingNamespace?.Name == "System");
        }
        
        private bool IsDisposed(MethodDeclarationSyntax method, string variableName, SemanticModel semanticModel)
        {
            // This is a simplified check and may have false positives/negatives
            
            // Look for using statements
            var usingStatements = method.DescendantNodes().OfType<UsingStatementSyntax>();
            foreach (var usingStatement in usingStatements)
            {
                if (usingStatement.Declaration != null)
                {
                    // Check declaration-form using
                    foreach (var variable in usingStatement.Declaration.Variables)
                    {
                        if (variable.Identifier.ValueText == variableName)
                            return true;
                    }
                }
                else if (usingStatement.Expression != null)
                {
                    // Check expression-form using
                    if (usingStatement.Expression is IdentifierNameSyntax identifierName && 
                        identifierName.Identifier.ValueText == variableName)
                    {
                        return true;
                    }
                }
            }
            
            // Look for direct calls to Dispose()
            var memberAccesses = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            foreach (var memberAccess in memberAccesses)
            {
                if (memberAccess.Name.Identifier.ValueText == "Dispose" && 
                    memberAccess.Expression is IdentifierNameSyntax identifierName && 
                    identifierName.Identifier.ValueText == variableName)
                {
                    // Check if it's actually invoked
                    if (memberAccess.Parent is InvocationExpressionSyntax)
                        return true;
                }
            }
            
            return false;
        }
        
        private bool IsReturnedOrPassedToMethod(MethodDeclarationSyntax method, string variableName, SemanticModel semanticModel)
        {
            // Check if the variable is returned or passed to another method
            
            // Check return statements
            var returnStatements = method.DescendantNodes().OfType<ReturnStatementSyntax>();
            foreach (var returnStatement in returnStatements)
            {
                if (returnStatement.Expression is IdentifierNameSyntax identifierName && 
                    identifierName.Identifier.ValueText == variableName)
                {
                    return true;
                }
            }
            
            // Check if passed to another method
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var arguments = invocation.ArgumentList.Arguments;
                foreach (var argument in arguments)
                {
                    if (argument.Expression is IdentifierNameSyntax identifierName && 
                        identifierName.Identifier.ValueText == variableName)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}