namespace MovementPass.Public.Api.BackgroundJob;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

using Services;

public interface IProcessor
{
    Task Process(
        IEnumerable<SQSEvent.SQSMessage> records,
        CancellationToken cancellationToken);
}

public class Processor : IProcessor
{
    private readonly IDataReducer _dataReducer;
    private readonly IDataLoader _dataLoader;

    public Processor(IDataReducer dataReducer, IDataLoader dataLoader)
    {
        this._dataReducer = dataReducer ??
                            throw new ArgumentNullException(
                                nameof(dataReducer));

        this._dataLoader = dataLoader ??
                           throw new ArgumentNullException(
                               nameof(dataLoader));
    }

    public async Task Process(
        IEnumerable<SQSEvent.SQSMessage> records,
        CancellationToken cancellationToken)
    {
        if (records == null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        var applications = this._dataReducer
            .Reduce(records);

        await this._dataLoader.Load(applications, cancellationToken)
            .ConfigureAwait(false);
    }
}