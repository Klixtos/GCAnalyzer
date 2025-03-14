using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace GCAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InterfaceMethodExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GCA004";
        
        // Category for the analyzer
        private const string Category = "Design";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.InterfaceMethodNoExceptionsTitle,
            messageFormat: Resources.InterfaceMethodNoExceptionsMessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.InterfaceMethodNoExceptionsDescription,
            helpLinkUri: AnalyzerConstants.GetHelpLink(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for analyzing method declarations in interfaces
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            
            // Get the containing type
            var containingType = methodDeclaration.Parent as TypeDeclarationSyntax;
            if (containingType == null)
                return;
                
            // Check if this is an interface
            if (!containingType.Modifiers.Any(SyntaxKind.InterfaceKeyword))
                return;
                
            // Methods in interfaces are implicitly public, but we'll check for explicit public modifier too
            if (!methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                !methodDeclaration.Modifiers.Any())
            {
                // Look for throws expressions (throw statements, throw expressions, or documented exceptions)
                bool hasThrowExpression = HasThrowExpression(methodDeclaration);
                bool hasDocumentedExceptions = HasDocumentedExceptions(methodDeclaration);
                
                if (hasThrowExpression || hasDocumentedExceptions)
                {
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
                    if (methodSymbol != null)
                    {
                        string methodName = methodSymbol.ToDisplayString();
                        var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
        
        private bool HasThrowExpression(MethodDeclarationSyntax methodDeclaration)
        {
            // Look for throw statements in the method body
            if (methodDeclaration.Body != null)
            {
                var throwStatements = methodDeclaration.Body.DescendantNodes().OfType<ThrowStatementSyntax>();
                if (throwStatements.Any())
                    return true;
                    
                var throwExpressions = methodDeclaration.Body.DescendantNodes().OfType<ThrowExpressionSyntax>();
                if (throwExpressions.Any())
                    return true;
            }
            
            // Check for expression-bodied members that throw
            if (methodDeclaration.ExpressionBody != null)
            {
                var throwExpressions = methodDeclaration.ExpressionBody.DescendantNodes().OfType<ThrowExpressionSyntax>();
                if (throwExpressions.Any())
                    return true;
            }
            
            return false;
        }
        
        private bool HasDocumentedExceptions(MethodDeclarationSyntax methodDeclaration)
        {
            // Check if the method has XML documentation with <exception> tags
            foreach (var trivia in methodDeclaration.GetLeadingTrivia())
            {
                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || 
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    var docComment = trivia.GetStructure() as DocumentationCommentTriviaSyntax;
                    if (docComment != null)
                    {
                        foreach (var xmlNode in docComment.Content)
                        {
                            if (xmlNode is XmlElementSyntax xmlElement && xmlElement.StartTag.Name.ToString() == "exception")
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }
    }
} 