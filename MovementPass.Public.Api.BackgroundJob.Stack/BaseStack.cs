namespace MovementPass.Public.Api.BackgroundJob.Stack;

using Constructs;
using Amazon.CDK;
using Amazon.CDK.AWS.SSM;

public abstract class BaseStack : Stack
{
    protected BaseStack(
        Construct scope,
        string id,
        IStackProps props = null) : base(scope, id, props)
    {
    }

    protected string App => this.GetContextValue<string>("app");

    protected string Version => this.GetContextValue<string>("version");

    protected string ConfigRootKey => $"/{this.App}/{this.Version}";

    protected T GetContextValue<T>(string key) =>
        (T)this.Node.TryGetContext(key);

    protected string GetParameterStoreValue(string name) =>
        StringParameter.ValueForStringParameter(
            this,
            $"{this.ConfigRootKey}/{name}");
}