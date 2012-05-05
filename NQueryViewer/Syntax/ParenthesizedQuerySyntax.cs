using System;
using System.Collections.Generic;

namespace NQueryViewer.Syntax
{
    public sealed class ParenthesizedQuerySyntax : QuerySyntax
    {
        private readonly SyntaxToken _leftParentheses;
        private readonly QuerySyntax _query;
        private readonly SyntaxToken _rightParentheses;

        public ParenthesizedQuerySyntax(SyntaxToken leftParentheses, QuerySyntax query, SyntaxToken rightParentheses)
        {
            _leftParentheses = leftParentheses;
            _query = query;
            _rightParentheses = rightParentheses;
        }

        public override SyntaxKind Kind
        {
            get { return SyntaxKind.ParenthesizedQuery; }
        }

        public override IEnumerable<SyntaxNodeOrToken> GetChildren()
        {
            yield return _leftParentheses;
            yield return _query;
            yield return _rightParentheses;
        }

        public SyntaxToken LeftParentheses
        {
            get { return _leftParentheses; }
        }

        public QuerySyntax Query
        {
            get { return _query; }
        }

        public SyntaxToken RightParentheses
        {
            get { return _rightParentheses; }
        }
    }
}