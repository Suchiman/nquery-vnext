using System;
using System.Collections.Generic;

namespace NQueryViewer.Syntax
{
    public sealed class LikeExpressionSyntax : ExpressionSyntax
    {
        private readonly ExpressionSyntax _left;
        private readonly SyntaxToken? _notKeyword;
        private readonly SyntaxToken _likeKeyword;
        private readonly ExpressionSyntax _right;

        public LikeExpressionSyntax(ExpressionSyntax left, SyntaxToken? notKeyword, SyntaxToken likeKeyword, ExpressionSyntax right)
        {
            _left = left;
            _notKeyword = notKeyword;
            _likeKeyword = likeKeyword;
            _right = right;
        }

        public override SyntaxKind Kind
        {
            get { return SyntaxKind.LikeExpression; }
        }

        public override IEnumerable<SyntaxNodeOrToken> GetChildren()
        {
            yield return _left;
            if (_notKeyword != null)
                yield return _notKeyword.Value;
            yield return _likeKeyword;
            yield return _right;
        }

        public ExpressionSyntax Left
        {
            get { return _left; }
        }

        public SyntaxToken? NotKeyword
        {
            get { return _notKeyword; }
        }

        public SyntaxToken LikeKeyword
        {
            get { return _likeKeyword; }
        }

        public ExpressionSyntax Right
        {
            get { return _right; }
        }
    }
}