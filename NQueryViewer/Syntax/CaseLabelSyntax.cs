using System;
using System.Collections.Generic;

namespace NQueryViewer.Syntax
{
    public sealed class CaseLabelSyntax : SyntaxNode
    {
        private readonly SyntaxToken _whenKeyword;
        private readonly ExpressionSyntax _whenExpression;
        private readonly SyntaxToken _thenKeyword;
        private readonly ExpressionSyntax _thenExpression;

        public CaseLabelSyntax(SyntaxToken whenKeyword, ExpressionSyntax whenExpression, SyntaxToken thenKeyword, ExpressionSyntax thenExpression)
        {
            _whenKeyword = whenKeyword;
            _whenExpression = whenExpression;
            _thenKeyword = thenKeyword;
            _thenExpression = thenExpression;
        }

        public override SyntaxKind Kind
        {
            get { return SyntaxKind.CaseLabel; }
        }

        public override IEnumerable<SyntaxNodeOrToken> GetChildren()
        {
            yield return _whenKeyword;
            yield return _whenExpression;
            yield return _thenKeyword;
            yield return _thenExpression;
        }

        public SyntaxToken WhenKeyword
        {
            get { return _whenKeyword; }
        }

        public ExpressionSyntax WhenExpression
        {
            get { return _whenExpression; }
        }

        public SyntaxToken ThenKeyword
        {
            get { return _thenKeyword; }
        }

        public ExpressionSyntax ThenExpression
        {
            get { return _thenExpression; }
        }
    }
}