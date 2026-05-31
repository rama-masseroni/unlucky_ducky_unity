public interface IExecutionStartValidator
{
    bool CanStartExecution(out string failureReason);
}
