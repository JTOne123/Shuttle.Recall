﻿using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Recall
{
    public class AssembleEventStreamObserver : IPipelineObserver<OnAssembleEventStream>
    {
        private readonly IEventMethodInvoker _eventMethodInvoker;

        public AssembleEventStreamObserver(IEventMethodInvoker eventMethodInvoker)
        {
            Guard.AgainstNull(eventMethodInvoker, nameof(eventMethodInvoker));

            _eventMethodInvoker = eventMethodInvoker;
        }

        public void Execute(OnAssembleEventStream pipelineEvent)
        {
            var state = pipelineEvent.Pipeline.State;

            state.SetEventStream(new EventStream(state.GetId(), state.GetVersion(), state.GetEvents(),
                _eventMethodInvoker));
        }
    }
}