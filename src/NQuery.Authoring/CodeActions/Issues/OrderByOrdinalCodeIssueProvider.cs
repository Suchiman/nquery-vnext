﻿using System;
using System.Collections.Generic;
using System.Linq;

using NQuery.Syntax;
using NQuery.Text;

namespace NQuery.Authoring.CodeActions.Issues
{
    internal sealed class OrderByOrdinalCodeIssueProvider : CodeIssueProvider<OrderedQuerySyntax>
    {
        protected override IEnumerable<CodeIssue> GetIssues(SemanticModel semanticModel, OrderedQuerySyntax node)
        {
            return from orderByColumn in node.Columns
                   let selector = orderByColumn.ColumnSelector as LiteralExpressionSyntax
                   where selector != null && selector.Value is int
                   let column = semanticModel.GetSymbol(orderByColumn)
                   where column != null && !string.IsNullOrEmpty(column.Name)
                   let namedReference = SyntaxFacts.GetValidIdentifier(column.Name)
                   let action = new[] {new ReplaceOrdingalByNamedReferenceCodeAction(selector, namedReference)}
                   select new CodeIssue(CodeIssueKind.Warning, selector.Span, action);
        }

        private sealed class ReplaceOrdingalByNamedReferenceCodeAction : CodeAction
        {
            private readonly ExpressionSyntax _selector;
            private readonly string _columnReference;

            public ReplaceOrdingalByNamedReferenceCodeAction(ExpressionSyntax selector, string columnReference)
                : base(selector.SyntaxTree)
            {
                _selector = selector;
                _columnReference = columnReference;
            }

            public override string Description
            {
                get { return Resources.CodeActionReplaceOrdinalByNamedColumn; }
            }

            protected override void GetChanges(TextChangeSet changeSet)
            {
                changeSet.ReplaceText(_selector.Span, _columnReference);
            }
        }
    }
}