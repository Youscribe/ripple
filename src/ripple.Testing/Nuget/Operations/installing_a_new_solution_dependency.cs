﻿using FubuTestingSupport;
using NUnit.Framework;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing.Nuget.Operations
{
    [TestFixture]
    public class installing_a_new_floating_solution_dependency : NugetOperationContext
    {
        private SolutionScenario theScenario;
        private Solution theSolution;
        private NugetPlan thePlan;
        private NugetPlanBuilder theBuilder;

        [SetUp]
        public void SetUp()
        {
            FeedScenario.Create(scenario => scenario.For(Feed.Fubu).Add("fubu", "1.0.0.1"));

            theScenario = SolutionScenario.Create(scenario => scenario.Solution("Test"));

            theSolution = theScenario.Find("Test");

            theBuilder = new NugetPlanBuilder();

            var request = new NugetPlanRequest
            {
                Solution = theSolution,
                Dependency = new Dependency("fubu", UpdateMode.Float),
                Operation = OperationType.Install
            };

            thePlan = theBuilder.PlanFor(request);
        }

        [TearDown]
        public void TearDown()
        {
            theScenario.Cleanup();
            FeedRegistry.Reset();
        }
        
        [Test]
        public void installs_to_the_solution()
        {
            thePlan.ShouldHaveTheSameElementsAs(
                solutionInstallation("fubu", "1.0.0.1", UpdateMode.Float)
            );
        }
    }
}