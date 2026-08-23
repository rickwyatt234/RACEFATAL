/*
    USEFUL FOR OPERATIONS SUCH AS
    SHOPSERVICE.PURCHASE()
    RESEARCHSERVICE.UNLOCK()
    WITHOUT THROWING EXCEPTIONS WHEN THE ID IS INVALID
*/

namespace RaceFatal.Shared
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public T Value { get; }

        private Result(bool isSuccess, string errorMessage, T value)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Value = value;
        }

        public static Result<T> Success(T value) => new Result<T>(true, null, value);
        public static Result<T> Failure(string errorMessage) => new Result<T>(false, errorMessage, default);
    }
}
