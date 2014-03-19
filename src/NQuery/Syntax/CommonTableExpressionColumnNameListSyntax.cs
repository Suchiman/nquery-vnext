using System;
using System.Collections.Generic;

namespace NQuery.Syntax
{
    public sealed class CommonTableExpressionColumnNameListSyntax : SyntaxNode
    {
        private readonly SyntaxToken _leftParenthesis;
        private readonly SeparatedSyntaxList<CommonTableExpressionColumnNameSyntax> _columnNames;
        private readonly SyntaxToken _rightParenthesis;

        public CommonTableExpressionColumnNameListSyntax(SyntaxTree syntaxTree, SyntaxToken leftParenthesis, SeparatedSyntaxList<CommonTableExpressionColumnNameSyntax> columnNames, SyntaxToken rightParenthesis)
            : base(syntaxTree)
        {
            _leftParenthesis = leftParenthesis;
            _columnNames = columnNames;
            _rightParenthesis = rightParenthesis;
        }

        public override SyntaxKind Kind
        {
            get { return SyntaxKind.CommonTableExpressionColumnNameList; }
        }

        public override IEnumerable<SyntaxNodeOrToken> ChildNodesAndTokens()
        {
            yield return _leftParenthesis;
            foreach (var nodeOrToken in _columnNames.GetWithSeparators())
                yield return nodeOrToken;
            yield return _rightParenthesis;
        }

        public SyntaxToken LeftParenthesis
        {
            get { return _leftParenthesis; }
        }

        public SeparatedSyntaxList<CommonTableExpressionColumnNameSyntax> ColumnNames
        {
            get { return _columnNames; }
        }

        public SyntaxToken RightParenthesis
        {
            get { return _rightParenthesis; }
        }
    }
}