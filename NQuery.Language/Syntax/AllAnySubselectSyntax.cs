using System;
using System.Collections.Generic;

namespace NQuery.Language
{
    public sealed class AllAnySubselectSyntax : SubselectExpressionSyntax
    {
        private readonly ExpressionSyntax _left;
        private readonly SyntaxToken _operatorToken;
        private readonly SyntaxToken _leftParenthesis;
        private readonly QuerySyntax _query;
        private readonly SyntaxToken _rightParenthesis;

        public AllAnySubselectSyntax(ExpressionSyntax left, SyntaxToken operatorToken, SyntaxToken leftParenthesis, QuerySyntax query, SyntaxToken rightParenthesis)
        {
            _left = left;
            _operatorToken = operatorToken;
            _leftParenthesis = leftParenthesis;
            _query = query;
            _rightParenthesis = rightParenthesis;
        }

        public override SyntaxKind Kind
        {
            get { return SyntaxKind.AllAnySubselect; }
        }

        public override IEnumerable<SyntaxNodeOrToken> GetChildren()
        {
            yield return _left;
            yield return _operatorToken;
            yield return _leftParenthesis;
            yield return _query;
            yield return _rightParenthesis;
        }

        public ExpressionSyntax Left
        {
            get { return _left; }
        }

        public SyntaxToken OperatorToken
        {
            get { return _operatorToken; }
        }

        public SyntaxToken LeftParenthesis
        {
            get { return _leftParenthesis; }
        }

        public QuerySyntax Query
        {
            get { return _query; }
        }

        public SyntaxToken RightParenthesis
        {
            get { return _rightParenthesis; }
        }
    }
}