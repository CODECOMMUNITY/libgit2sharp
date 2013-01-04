﻿using System;
using System.IO;
        [Fact]
        [Fact]

        [Fact]
        public void ComparingReliesOnProvidedConfigEntriesIfAny()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            const string file = "1/branch_file.txt";

            using (var repo = new Repository(path.DirectoryPath))
            {
                TreeEntry entry = repo.Head[file];
                Assert.Equal(Mode.ExecutableFile, entry.Mode);

                // Recreate the file in the workdir without the executable bit
                string fullpath = Path.Combine(repo.Info.WorkingDirectory, file);
                File.Delete(fullpath);
                File.WriteAllBytes(fullpath, ((Blob)(entry.Target)).Content);

                // Unset the local core.filemode, if any.
                repo.Config.Unset("core.filemode", ConfigurationLevel.Local);
            }

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            var options = BuildFakeSystemConfigFilemodeOption(scd, true);

            using (var repo = new Repository(path.DirectoryPath, options))
            {
                TreeChanges changes = repo.Diff.Compare(new []{ file });

                Assert.Equal(1, changes.Count());

                var change = changes.Modified.Single();
                Assert.Equal(Mode.ExecutableFile, change.OldMode);
                Assert.Equal(Mode.NonExecutableFile, change.Mode);
            }

            options = BuildFakeSystemConfigFilemodeOption(scd, false);

            using (var repo = new Repository(path.DirectoryPath, options))
            {
                TreeChanges changes = repo.Diff.Compare(new[] { file });

                Assert.Equal(0, changes.Count());
            }
        }

        private RepositoryOptions BuildFakeSystemConfigFilemodeOption(
            SelfCleaningDirectory scd,
            bool value)
        {
            Directory.CreateDirectory(scd.DirectoryPath);

            var options = new RepositoryOptions
                              {
                                  SystemConfigurationLocation = Path.Combine(
                                      scd.RootedDirectoryPath, "fake-system.config")
                              };

            StringBuilder sb = new StringBuilder()
                .AppendFormat("[core]{0}", Environment.NewLine)
                .AppendFormat("filemode = {1}{0}", Environment.NewLine, value);
            File.WriteAllText(options.SystemConfigurationLocation, sb.ToString());

            return options;
        }