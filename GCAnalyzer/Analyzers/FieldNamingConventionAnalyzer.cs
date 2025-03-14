using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace GCAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FieldNamingConventionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GCA005";
        
        // Category for the analyzer
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.FieldNamingConventionTitle,
            messageFormat: Resources.FieldNamingConventionMessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.FieldNamingConventionDescription,
            helpLinkUri: AnalyzerConstants.GetHelpLink(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for analyzing field declarations
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
            
            // Skip fields with static, const, or readonly modifiers (these often have different naming conventions)
            if (fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) || 
                fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword) ||
                fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                return;
            }

            // Get the containing type
            var containingType = fieldDeclaration.Parent as TypeDeclarationSyntax;
            if (containingType == null)
                return;
                
            // Skip fields in interfaces and enums
            if (containingType.Kind() == SyntaxKind.InterfaceDeclaration || 
                containingType.Kind() == SyntaxKind.EnumDeclaration)
            {
                return;
            }

            // Skip non-private fields unless they're in a private class
            bool isContainingTypePrivate = containingType.Modifiers.Any(SyntaxKind.PrivateKeyword);
            if (!fieldDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword) && !isContainingTypePrivate)
            {
                // Skip public/protected/internal fields
                return;
            }

            // Check each variable declared in the field
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                string fieldName = variable.Identifier.ValueText;
                
                // Check if field name follows the convention: starts with underscore and is camelCase
                if (!IsValidFieldName(fieldName))
                {
                    var diagnostic = Diagnostic.Create(Rule, variable.Identifier.GetLocation(), fieldName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Checks if a field name follows the underscore prefix + camelCase naming convention.
        /// </summary>
        private bool IsValidFieldName(string fieldName)
        {
            // Must start with underscore
            if (string.IsNullOrEmpty(fieldName) || !fieldName.StartsWith("_"))
                return false;

            // Skip just the underscore
            if (fieldName.Length == 1)
                return false;

            // Check that first character after underscore is lowercase
            if (char.IsUpper(fieldName[1]))
                return false;

            // Check that it only contains valid characters
            return Regex.IsMatch(fieldName, @"^_[a-z][a-zA-Z0-9]*$");
        }
    }
} 