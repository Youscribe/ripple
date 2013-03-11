﻿using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using FubuCore.Util;
using ripple.New.Model;

namespace ripple.New.Nuget
{
    public class NugetFolderCache : INugetCache
    {
        private readonly string _folder;
	    private IFileSystem _fileSystem;

	    private Lazy<IEnumerable<INugetFile>> _allFiles;

        public NugetFolderCache(string folder)
        {
            _folder = folder;
			_fileSystem = new FileSystem();

	        reset();
        }

		private void reset()
		{
			_allFiles = new Lazy<IEnumerable<INugetFile>>(findNugetFiles);
		}

		private IEnumerable<INugetFile> findNugetFiles()
		{
			return _fileSystem
				.FindFiles(_folder, new FileSet { Include = "*.nupkg" })
				.Select(file => new NugetFile(file))
				.ToList();
		}

	    private IEnumerable<INugetFile> Dependencies
	    {
			get { return _allFiles.Value; }
	    }

		public void UseFileSystem(IFileSystem fileSystem)
		{
			_fileSystem = fileSystem;
		}

	    public void Update(IRemoteNuget nuget)
	    {
		    UpdateAll(new[] { nuget });
	    }

	    public void UpdateAll(IEnumerable<IRemoteNuget> nugets)
        {
            var latest = new Cache<string, Version>(name => new Version("0.0.0.0"));
            Dependencies.GroupBy(x => x.Name).Each(x => {
                var version = x.OrderByDescending(file => file.Version).First().Version.Version;
                latest[x.Key] = version;
            });

            nugets.Each(nuget => {
                var localVersion = latest[nuget.Name];
                var remoteVersion = nuget.Version.Version;

                if (remoteVersion > localVersion)
                {
                    Console.WriteLine("Downloading {0} to {1}", nuget.Filename, _folder);
                    nuget.DownloadTo(_folder);
                }
            });

		    reset();
        }

		public INugetFile Latest(Dependency query)
        {
            IEnumerable<INugetFile> files = Dependencies.Where(x => x.Name == query.Name).ToList();
            if (query.Stability == NugetStability.ReleasedOnly)
            {
                files = files.Where(x => x.Version.SpecialVersion.IsEmpty());
            }

            return files.OrderByDescending(x => x.Version).FirstOrDefault();
        }

        public void Flush()
        {
            _fileSystem.CleanDirectory(_folder);
        }

        public IEnumerable<INugetFile> AllFiles()
        {
	        return Dependencies;
        }

		public virtual INugetFile Find(Dependency query)
        {
            return Dependencies.FirstOrDefault(x => x.Name == query.Name && x.Version.Version.ToString() == query.Version);
        }

	    public IRemoteNuget Retrieve(IRemoteNuget nuget)
	    {
		    if (nuget is CachedNuget)
		    {
			    return nuget;
		    }

		    var dependency = new Dependency(nuget.Name, nuget.Version.Version.ToString());
		    var file = Find(dependency);

			if (file == null)
			{
				return nuget;
			}

		    return new CachedNuget(file);
	    }
    }
}