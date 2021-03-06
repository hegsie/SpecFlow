using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BoDi;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;

#if SILVERLIGHT
using TechTalk.SpecFlow.Compatibility;
#endif

namespace TechTalk.SpecFlow
{
    public class ScenarioContext : SpecFlowContext
    {
        #region Singleton
        private static bool isCurrentDisabled = false;
        private static ScenarioContext current;
        public static ScenarioContext Current
        {
            get
            {
                if (isCurrentDisabled)
                    throw new SpecFlowException("The ScenarioContext.Current static accessor cannot be used in multi-threaded execution. Try injecting the scenario context to the binding class. See http://go.specflow.org/doc-multithreaded for details.");
                if (current == null)
                {
                    Debug.WriteLine("Accessing NULL ScenarioContext");
                }
                return current;
            }
            internal set
            {
                if (!isCurrentDisabled)
                    current = value;
            }
        }

        internal static void DisableSingletonInstance()
        {
            isCurrentDisabled = true;
            Thread.MemoryBarrier();
            current = null;
        }
        #endregion

        public ScenarioInfo ScenarioInfo { get; private set; }
        public ScenarioBlock CurrentScenarioBlock { get; internal set; }
        public Exception TestError { get; internal set; }

        internal TestStatus TestStatus { get; set; }
        internal List<string> PendingSteps { get; private set; }
        internal List<StepInstance> MissingSteps { get; private set; }
        internal Stopwatch Stopwatch { get; private set; }

        private readonly IObjectContainer scenarioContainer;
        private readonly IBindingInstanceResolver bindingInstanceResolver;

        public IObjectContainer ScenarioContainer
        {
            get { return scenarioContainer; }
        }

        internal ScenarioContext(IObjectContainer scenarioContainer, ScenarioInfo scenarioInfo, IBindingInstanceResolver bindingInstanceResolver)
        {
            this.scenarioContainer = scenarioContainer;
            this.bindingInstanceResolver = bindingInstanceResolver;

            Stopwatch = new Stopwatch();
            Stopwatch.Start();

            CurrentScenarioBlock = ScenarioBlock.None;
            ScenarioInfo = scenarioInfo;
            TestStatus = TestStatus.OK;
            PendingSteps = new List<string>();
            MissingSteps = new List<StepInstance>();
        }

        public ScenarioStepContext StepContext
        {
            get
            {
                var contextManager = ScenarioContainer.Resolve<IContextManager>();
                return contextManager.StepContext;
            }
        }

        public void Pending()
        {
            throw new PendingStepException();
        }

        /// <summary>
        /// Called by SpecFlow infrastructure when an instance of a binding class is needed.
        /// </summary>
        /// <param name="bindingType">The type of the binding class.</param>
        /// <returns>The binding class instance</returns>
        /// <remarks>
        /// The binding classes are the classes with the [Binding] attribute, that might 
        /// contain step definitions, hooks or step argument transformations. The method 
        /// is called when any binding method needs to be called.
        /// </remarks>
        public object GetBindingInstance(Type bindingType)
        {
            return bindingInstanceResolver.ResolveBindingInstance(bindingType, scenarioContainer);
        }

        private bool isDisposed = false;
        protected override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true; //HACK: we need this flag, because the ScenarioContainer is disposed by the scenarioContextManager of the IContextManager and while we dispose the container itself, the it will call the dispose on us again...
            base.Dispose();

            scenarioContainer.Dispose();
        }
    }
}