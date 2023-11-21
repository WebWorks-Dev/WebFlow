namespace WebFlow.Models;

public class WebFlowException : Exception
{
    public WebFlowException(string errorMessage) : base(errorMessage) {}
}