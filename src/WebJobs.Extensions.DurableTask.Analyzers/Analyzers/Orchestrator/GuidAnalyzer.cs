﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GuidAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DF0102";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.GuidAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.DeterministicAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DeterministicAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = SupportedCategories.Orchestrator;
        public const DiagnosticSeverity severity = DiagnosticSeverity.Warning;

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, severity, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierGuid, SyntaxKind.IdentifierName);
        }

        private static void AnalyzeIdentifierGuid(SyntaxNodeAnalysisContext context)
        {
            var identifierName = context.Node as IdentifierNameSyntax;
            if (identifierName != null)
            {
                if (identifierName.Identifier.ValueText == "NewGuid")
                {
                    var memberAccessExpression = identifierName.Parent;
                    var invocationExpression = memberAccessExpression.Parent;
                    var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol;

                    if (!memberSymbol?.ToString().StartsWith("System.Guid") ?? true)
                    {
                        return;
                    }
                    else if (!SyntaxNodeUtils.IsInsideOrchestrator(identifierName) && !SyntaxNodeUtils.IsMarkedDeterministic(identifierName))
                    {
                        return;
                    }
                    else
                    {
                        var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation(), memberAccessExpression);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
