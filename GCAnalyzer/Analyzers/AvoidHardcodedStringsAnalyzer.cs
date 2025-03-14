using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace GCAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AvoidHardcodedStringsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GCA006";
        
        private const string Category = "Maintainability";
        
        // Configure minimum string length to trigger the rule
        private const int MinimumStringLength = 3;
        
        // Strings that are typically allowed as hardcoded
        private static readonly string[] AllowedStrings = new string[]
        {
            string.Empty,
            " ",
            "\\n",
            "\\r",
            "\\t",
            "\n",
            "\r",
            "\t",
            ",",
            ".",
            ":",
            ";",
            "-",
            "_",
            "=",
            "+",
            "*",
            "/",
            "\\",
            "(",
            ")",
            "[",
            "]",
            "{",
            "}",
            "<",
            ">",
            "\"",
            "'",
            "`"
        };

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.AvoidHardcodedStringsTitle,
            messageFormat: Resources.AvoidHardcodedStringsMessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Resources.AvoidHardcodedStringsDescription,
            helpLinkUri: AnalyzerConstants.GetHelpLink(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for string literal expressions
            context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
        }

        private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
        {
            var stringLiteral = (LiteralExpressionSyntax)context.Node;
            string value = stringLiteral.Token.ValueText;
            
            // Skip empty strings, small strings, or allowed strings
            if (string.IsNullOrWhiteSpace(value) || 
                value.Length < MinimumStringLength || 
                AllowedStrings.Contains(value))
            {
                return;
            }
            
            // Skip strings used in attributes and XML doc comments
            if (IsInAttributeContext(stringLiteral) || IsInXmlDocContext(stringLiteral))
            {
                return;
            }
            
            // Skip strings used in unit tests
            if (IsInTestContext(stringLiteral, context.SemanticModel))
            {
                return;
            }
            
            // Report diagnostic for hardcoded strings
            var diagnostic = Diagnostic.Create(Rule, stringLiteral.GetLocation(), value);
            context.ReportDiagnostic(diagnostic);
        }
        
        private bool IsInAttributeContext(LiteralExpressionSyntax stringLiteral)
        {
            return stringLiteral.Ancestors().Any(a => a is AttributeSyntax);
        }
        
        private bool IsInXmlDocContext(LiteralExpressionSyntax stringLiteral)
        {
            return stringLiteral.Ancestors().Any(a => a is DocumentationCommentTriviaSyntax);
        }
        
        private bool IsInTestContext(LiteralExpressionSyntax stringLiteral, SemanticModel semanticModel)
        {
            // Check if we're in a test method/class (has test attributes)
            var methodDecl = stringLiteral.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDecl != null)
            {
                foreach (var attributeList in methodDecl.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var attributeName = attribute.Name.ToString();
                        if (attributeName.Contains("Test") || attributeName.Contains("Fact"))
                        {
                            return true;
                        }
                    }
                }
            }
            
            // Check if the containing class has test attributes
            var classDecl = stringLiteral.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDecl != null)
            {
                foreach (var attributeList in classDecl.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var attributeName = attribute.Name.ToString();
                        if (attributeName.Contains("Test") || attributeName.Contains("Fact"))
                        {
                            return true;
                        }
                    }
                }
                
                // Check if class name indicates it's a test class
                var className = classDecl.Identifier.ValueText;
                if (className.EndsWith("Test") || className.EndsWith("Tests") || 
                    className.StartsWith("Test") || className.Contains("Tests"))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
} 