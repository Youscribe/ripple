﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FubuCore.Descriptions;
using ripple.New.Commands;
using ripple.New.Model;
using ripple.New.Nuget;

namespace ripple.New.Steps
{
	public class DownloadMissingNugets : IRippleStep, DescribesItself
	{
		public Solution Solution { get; set; }

		public void Execute(SolutionInput input, IRippleStepRunner runner)
		{
			var missing = Solution.MissingNugets().ToList();
			var nugets = new List<INugetFile>();

			if (missing.Any())
			{
				var tasks = missing.Select(x => restore(x, Solution, nugets)).ToArray();

				Task.WaitAll(tasks);
			}

			runner.Set(new DownloadedNugets(nugets));
		}

		private static Task restore(Dependency query, Solution solution, List<INugetFile> nugets)
		{
			return Task.Factory.StartNew(() =>
			{
				var nuget = solution.Restore(query);

				RippleLog.Debug("Downloading " + nuget);
				nugets.Add(nuget.DownloadTo(solution.PackagesDirectory()));

				solution.Cache.Update(nuget);
			});
		}

		public void Describe(Description description)
		{
			description.ShortDescription = "Download missing nugets for " + Solution.Name;
		}
	}
}