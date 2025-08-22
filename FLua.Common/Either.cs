using System;

namespace FLua.Common
{
    /// <summary>
    /// Represents a value that can be one of two types (Left or Right).
    /// Commonly used where Left represents an error and Right represents success.
    /// </summary>
    public readonly struct Either<TLeft, TRight>
    {
        private readonly TLeft? _left;
        private readonly TRight? _right;
        private readonly bool _isRight;

        private Either(TLeft left)
        {
            _left = left;
            _right = default;
            _isRight = false;
        }

        private Either(TRight right)
        {
            _left = default;
            _right = right;
            _isRight = true;
        }

        /// <summary>
        /// Gets whether this Either contains a Left value
        /// </summary>
        public bool IsLeft => !_isRight;

        /// <summary>
        /// Gets whether this Either contains a Right value
        /// </summary>
        public bool IsRight => _isRight;

        /// <summary>
        /// Gets the Left value. Throws if this Either contains a Right value.
        /// </summary>
        public TLeft Left => !_isRight ? _left! : throw new InvalidOperationException("Either contains Right value, not Left");

        /// <summary>
        /// Gets the Right value. Throws if this Either contains a Left value.
        /// </summary>
        public TRight Right => _isRight ? _right! : throw new InvalidOperationException("Either contains Left value, not Right");

        /// <summary>
        /// Creates an Either containing a Left value
        /// </summary>
        public static Either<TLeft, TRight> FromLeft(TLeft left) => new(left);

        /// <summary>
        /// Creates an Either containing a Right value
        /// </summary>
        public static Either<TLeft, TRight> FromRight(TRight right) => new(right);

        /// <summary>
        /// Attempts to get the Left value
        /// </summary>
        public bool TryGetLeft(out TLeft left)
        {
            if (!_isRight)
            {
                left = _left!;
                return true;
            }
            left = default!;
            return false;
        }

        /// <summary>
        /// Attempts to get the Right value
        /// </summary>
        public bool TryGetRight(out TRight right)
        {
            if (_isRight)
            {
                right = _right!;
                return true;
            }
            right = default!;
            return false;
        }

        /// <summary>
        /// Executes an action based on whether this Either contains Left or Right
        /// </summary>
        public void Match(Action<TLeft> onLeft, Action<TRight> onRight)
        {
            if (_isRight)
                onRight(_right!);
            else
                onLeft(_left!);
        }

        /// <summary>
        /// Transforms this Either based on whether it contains Left or Right
        /// </summary>
        public TResult Match<TResult>(Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)
        {
            return _isRight ? onRight(_right!) : onLeft(_left!);
        }

        /// <summary>
        /// Maps the Right value to a new value, leaving Left unchanged
        /// </summary>
        public Either<TLeft, TNewRight> Map<TNewRight>(Func<TRight, TNewRight> mapper)
        {
            return _isRight 
                ? Either<TLeft, TNewRight>.FromRight(mapper(_right!))
                : Either<TLeft, TNewRight>.FromLeft(_left!);
        }

        /// <summary>
        /// Maps the Left value to a new value, leaving Right unchanged
        /// </summary>
        public Either<TNewLeft, TRight> MapLeft<TNewLeft>(Func<TLeft, TNewLeft> mapper)
        {
            return _isRight 
                ? Either<TNewLeft, TRight>.FromRight(_right!)
                : Either<TNewLeft, TRight>.FromLeft(mapper(_left!));
        }

        /// <summary>
        /// Chains another Either-producing operation on the Right value
        /// </summary>
        public Either<TLeft, TNewRight> Bind<TNewRight>(Func<TRight, Either<TLeft, TNewRight>> binder)
        {
            return _isRight ? binder(_right!) : Either<TLeft, TNewRight>.FromLeft(_left!);
        }

        /// <summary>
        /// Implicit conversion from Left type
        /// </summary>
        public static implicit operator Either<TLeft, TRight>(TLeft left) => FromLeft(left);

        /// <summary>
        /// Implicit conversion from Right type
        /// </summary>
        public static implicit operator Either<TLeft, TRight>(TRight right) => FromRight(right);

        /// <summary>
        /// Converts this Either to a Result where Left becomes Failure and Right becomes Success
        /// </summary>
        public Result<TRight> ToResult()
        {
            return _isRight 
                ? Result<TRight>.Success(_right!)
                : Result<TRight>.Failure(_left?.ToString() ?? "Unknown error");
        }

        /// <summary>
        /// Returns a string representation of this Either
        /// </summary>
        public override string ToString()
        {
            return _isRight ? $"Right({_right})" : $"Left({_left})";
        }
    }

    /// <summary>
    /// Utility methods for working with Either
    /// </summary>
    public static class Either
    {
        /// <summary>
        /// Creates an Either containing a Left value
        /// </summary>
        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left) => Either<TLeft, TRight>.FromLeft(left);

        /// <summary>
        /// Creates an Either containing a Right value
        /// </summary>
        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right) => Either<TLeft, TRight>.FromRight(right);
    }
}