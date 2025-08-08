using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace AncestralMod.Utils;

public class GitService
{
	private readonly string _repositoryUrl;
	private readonly string _repoPath;

	public GitService(string repositoryUrl, string repoPath)
	{
		_repositoryUrl = repositoryUrl;
		_repoPath = repoPath;
	}

	public bool IsRepository()
	{
		Debug.Log($"[GitService] Checking if {_repoPath} is a valid repository...");
		
		bool dirExists = Directory.Exists(_repoPath);
		Debug.Log($"[GitService] Directory exists: {dirExists}");
		
		if (!dirExists)
		{
			return false;
		}
		
		bool isValid = Repository.IsValid(_repoPath);
		Debug.Log($"[GitService] Repository.IsValid: {isValid}");
		
		return isValid;
	}

	// Example async wrapper for Clone (runs on a background thread)
	public async Task<bool> CloneAsync()
	{
		return await Task.Run(() => Clone());
	}

	// Example async wrapper for Pull (runs on a background thread)
	public async Task<bool> PullAsync()
	{
		return await Task.Run(() => Pull());
	}

	// Example async wrapper for NeedsPull (runs on a background thread)
	public async Task<bool> NeedsPullAsync()
	{
		return await Task.Run(() => NeedsPull());
	}

	// Async wrapper for CheckoutFiles
	public async Task<bool> CheckoutFilesAsync()
	{
		return await Task.Run(() => CheckoutFiles());
	}

	public bool Clone()
	{
		try
		{
			Debug.Log($"[GitService] Starting clone operation...");
			Debug.Log($"[GitService] Repository URL: {_repositoryUrl}");
			Debug.Log($"[GitService] Target path: {_repoPath}");
			
			if (Directory.Exists(_repoPath))
			{
				Debug.Log($"[GitService] Target directory exists, deleting: {_repoPath}");
				var existingFiles = Directory.GetFiles(_repoPath, "*", SearchOption.AllDirectories);
				Debug.Log($"[GitService] Found {existingFiles.Length} existing files to delete");
				Directory.Delete(_repoPath, true);
				Debug.Log($"[GitService] Directory deleted successfully");
			}
			else
			{
				Debug.Log($"[GitService] Target directory does not exist, will be created by clone");
			}

		var cloneOptions = new CloneOptions
		{
			IsBare = false,
			Checkout = true,
			RecurseSubmodules = false
		};
		
		// Configure certificate validation to avoid SSL issues
		cloneOptions.FetchOptions.CertificateCheck = (certificate, valid, host) =>
		{
			return true;
		};
		
		// Add progress reporting for fetch/download operation
		cloneOptions.FetchOptions.OnProgress = (output) =>
		{
			Debug.Log($"[GitService] Clone progress: {output}");
			return true;
		};
		
		cloneOptions.FetchOptions.OnTransferProgress = (progress) =>
		{
			if (progress.TotalObjects > 0)
			{
				var percentage = progress.ReceivedObjects * 100 / progress.TotalObjects;
				Debug.Log($"[GitService] Download progress: {percentage}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
				
				// Show progress in UI
				try
				{
					var module = Modules.BetterBugleModule.Instance;
					if (module != null)
					{
						UI.BetterBugleUI.Instance?.ShowActionbar($"Downloading: {percentage}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"[GitService] Could not update UI progress: {ex.Message}");
				}
			}
			return true;
		};
		
		cloneOptions.OnCheckoutProgress = (path, completedSteps, totalSteps) =>
		{
			if (totalSteps > 0)
			{
				var percentage = completedSteps * 100 / totalSteps;
				Debug.Log($"[GitService] Checkout progress: {percentage}% ({completedSteps}/{totalSteps} files)");
				
				// Show checkout progress in UI
				try
				{
					var module = Modules.BetterBugleModule.Instance;
					if (module != null)
					{
						UI.BetterBugleUI.Instance?.ShowActionbar($"Extracting: {percentage}% ({completedSteps}/{totalSteps} files)");
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"[GitService] Could not update UI checkout progress: {ex.Message}");
				}
			}
		};			Debug.Log($"[GitService] Clone options - IsBare: {cloneOptions.IsBare}, Checkout: {cloneOptions.Checkout}, RecurseSubmodules: {cloneOptions.RecurseSubmodules}");
			Debug.Log($"[GitService] Calling Repository.Clone...");

			Repository.Clone(_repositoryUrl, _repoPath, cloneOptions);
			Debug.Log($"[GitService] Repository.Clone completed successfully");
			Debug.Log($"[GitService] Repository cloned to {_repoPath}");
			
			// Verify that directory was created
			if (!Directory.Exists(_repoPath))
			{
				Debug.LogError($"[GitService] CRITICAL: Repository directory was not created at {_repoPath}");
				return false;
			}
			
			Debug.Log($"[GitService] Repository directory exists, checking contents...");
			
			// Check for .git directory
			var gitDir = Path.Combine(_repoPath, ".git");
			if (Directory.Exists(gitDir))
			{
				Debug.Log($"[GitService] .git directory found at {gitDir}");
			}
			else
			{
				Debug.LogWarning($"[GitService] .git directory NOT found at {gitDir}");
			}
			
			// Get all files in the repository
			var allFiles = Directory.GetFiles(_repoPath, "*", SearchOption.AllDirectories);
			Debug.Log($"[GitService] Total files found in repository: {allFiles.Length}");
			
			// Get non-git files
			var files = allFiles.Where(f => !f.Contains(".git")).ToArray();
			Debug.Log($"[GitService] Non-git files found: {files.Length}");
			
			// Log some example files if any exist
			if (files.Length > 0)
			{
				Debug.Log($"[GitService] Example files:");
				for (int i = 0; i < Math.Min(files.Length, 5); i++)
				{
					var relativePath = Path.GetRelativePath(_repoPath, files[i]);
					var fileSize = new FileInfo(files[i]).Length;
					Debug.Log($"[GitService]   - {relativePath} ({fileSize} bytes)");
				}
				if (files.Length > 5)
				{
					Debug.Log($"[GitService]   ... and {files.Length - 5} more files");
				}
			}
			else
			{
				Debug.LogWarning($"[GitService] NO non-git files found after clone!");
				
				// List all files including git files for debugging
				Debug.Log($"[GitService] All files (including .git):");
				foreach (var file in allFiles.Take(10))
				{
					var relativePath = Path.GetRelativePath(_repoPath, file);
					var fileSize = new FileInfo(file).Length;
					Debug.Log($"[GitService]   - {relativePath} ({fileSize} bytes)");
				}
			}
			
			Debug.Log($"[GitService] Clone operation completed with {files.Length} content files");
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError($"Clone failed: {ex.Message}");
			return false;
		}
	}

	public bool NeedsPull()
	{
		Debug.Log($"[GitService] Checking if pull is needed...");
		
		if (!IsRepository())
		{
			Debug.Log($"[GitService] Not a repository, pull not needed");
			return false;
		}

		try
		{
			using var repo = new Repository(_repoPath);
			
			var remote = repo.Network.Remotes["origin"];
			if (remote == null)
			{
				return false;
			}
			
			Debug.Log($"[GitService] Origin remote found: {remote.Url}");
			var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
			
			var fetchOptions = new FetchOptions
			{
				CertificateCheck = (certificate, valid, host) =>
				{
					Debug.Log($"[GitService] Certificate check for {host}: Valid={valid}");
					return true;
				},
				OnProgress = (output) =>
				{
					Debug.Log($"[GitService] Fetch progress: {output}");
					return true;
				},
				OnTransferProgress = (progress) =>
				{
					if (progress.TotalObjects > 0)
					{
						var percentage = progress.ReceivedObjects * 100 / progress.TotalObjects;
						Debug.Log($"[GitService] Checking for updates: {percentage}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");

						try
						{
							var module = Modules.BetterBugleModule.Instance;
							if (module != null)
							{
								UI.BetterBugleUI.Instance?.ShowActionbar($"Checking updates: {percentage}%");
							}
						}
						catch (Exception ex)
						{
							Debug.LogWarning($"[GitService] Could not update UI fetch progress: {ex.Message}");
						}
					}
					return true;
				}
			};
			
			Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, "");

			var localCommit = repo.Head.Tip;
			var remoteBranchName = $"origin/{repo.Head.FriendlyName}";
			var remoteBranch = repo.Branches[remoteBranchName];
			var remoteCommit = remoteBranch?.Tip;

			bool needsPull = remoteCommit != null && localCommit?.Sha != remoteCommit.Sha;
			
			return needsPull;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool Pull()
	{
		Debug.Log($"[GitService] Starting pull operation...");
		
		if (!IsRepository())
		{
			Debug.LogError($"[GitService] Cannot pull: not a valid repository");
			return false;
		}

		try
		{
			using var repo = new Repository(_repoPath);
			Debug.Log($"[GitService] Repository opened for pull");
			
			var signature = new Signature("AncestralMod", "mod@example.com", DateTimeOffset.Now);
			Debug.Log($"[GitService] Using signature: {signature.Name} <{signature.Email}>");
			
			var pullOptions = new PullOptions
			{
				FetchOptions = new FetchOptions
				{
					CertificateCheck = (certificate, valid, host) =>
					{
						Debug.Log($"[GitService] Certificate check for {host}: Valid={valid}");
						return true;
					},
					OnProgress = (output) =>
					{
						Debug.Log($"[GitService] Pull progress: {output}");
						return true;
					},
					OnTransferProgress = static (progress) =>
					{
						if (progress.TotalObjects > 0)
						{
							var percentage = progress.ReceivedObjects * 100 / progress.TotalObjects;
							Debug.Log($"[GitService] Pull download progress: {percentage}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
							
							// Show progress in UI
							try
							{
								var module = Modules.BetterBugleModule.Instance;
								if (module != null)
								{
									UI.BetterBugleUI.Instance?.ShowActionbar($"Updating: {percentage}% ({progress.ReceivedObjects}/{progress.TotalObjects} objects)");
								}
							}
							catch (Exception ex)
							{
								Debug.LogWarning($"[GitService] Could not update UI pull progress: {ex.Message}");
							}
						}
						return true;
					}
				},
				MergeOptions = new MergeOptions()
			};
			
			Debug.Log($"[GitService] Executing pull...");
			var result = Commands.Pull(repo, signature, pullOptions);
			Debug.Log($"[GitService] Pull completed with status: {result.Status}");
			
			if (result.Commit != null)
			{
				Debug.Log($"[GitService] Pull result commit: {result.Commit.Sha}");
				Debug.Log($"[GitService] Pull result message: {result.Commit.MessageShort}");
			}
			
			// Verify that files are present after pull
			var successStatuses = new[] { 
				MergeStatus.UpToDate, 
				MergeStatus.FastForward, 
				MergeStatus.NonFastForward 
			};
			
			if (successStatuses.Contains(result.Status))
			{
				var files = Directory.GetFiles(_repoPath, "*", SearchOption.AllDirectories)
					.Where(f => !f.Contains(".git"))
					.ToArray();
				Debug.Log($"[GitService] Repository now contains {files.Length} non-git files after pull");
				
				if (files.Length > 0)
				{
					Debug.Log($"[GitService] Sample files after pull:");
					for (int i = 0; i < Math.Min(files.Length, 3); i++)
					{
						var relativePath = Path.GetRelativePath(_repoPath, files[i]);
						var fileSize = new FileInfo(files[i]).Length;
						Debug.Log($"[GitService]   - {relativePath} ({fileSize} bytes)");
					}
				}
				
				return true;
			}
			else
			{
				Debug.LogWarning($"[GitService] Pull status indicates failure: {result.Status}");
				return false;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError($"[GitService] Pull failed with exception: {ex.GetType().Name}");
			Debug.LogError($"[GitService] Exception message: {ex.Message}");
			Debug.LogError($"[GitService] Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Debug.LogError($"[GitService] Inner exception: {ex.InnerException.Message}");
			}
			return false;
		}
	}

	/// <summary>
	/// Explicitly checkout all files from the repository if they're missing
	/// </summary>
	public bool CheckoutFiles()
	{
		Debug.Log($"[GitService] Starting explicit checkout operation...");
		
		if (!IsRepository())
		{
			Debug.LogError($"[GitService] Cannot checkout: {_repoPath} is not a valid repository");
			return false;
		}

		try
		{
			Debug.Log($"[GitService] Opening repository at {_repoPath}");
			using var repo = new Repository(_repoPath);
			
			Debug.Log($"[GitService] Repository status:");
			Debug.Log($"[GitService]   - Head: {repo.Head?.FriendlyName ?? "null"}");
			Debug.Log($"[GitService]   - Is bare: {repo.Info.IsBare}");
			Debug.Log($"[GitService]   - Is headless: {repo.Info.IsHeadUnborn}");
			Debug.Log($"[GitService]   - Working directory: {repo.Info.WorkingDirectory ?? "null"}");
			
			if (repo.Head?.Tip != null)
			{
				Debug.Log($"[GitService]   - Head commit: {repo.Head.Tip.Sha}");
				Debug.Log($"[GitService]   - Head message: {repo.Head.Tip.MessageShort}");
			}
			else
			{
				Debug.LogError($"[GitService] Repository head or tip is null - cannot checkout");
				return false;
			}
			
			// List branches
			Debug.Log($"[GitService] Available branches:");
			foreach (var branch in repo.Branches)
			{
				Debug.Log($"[GitService]   - {branch.FriendlyName} (Remote: {branch.IsRemote}, Tracking: {branch.IsTracking})");
			}
			
			var checkoutOptions = new CheckoutOptions
			{
				CheckoutModifiers = CheckoutModifiers.Force,
				CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
				OnCheckoutNotify = (path, flags) => {
					Debug.Log($"[GitService] Checkout notify: {path} ({flags})");
					return true;
				},
				OnCheckoutProgress = (path, completedSteps, totalSteps) => {
					Debug.Log($"[GitService] Checkout progress: {path} ({completedSteps}/{totalSteps})");
				}
			};
			
			Debug.Log($"[GitService] Attempting to checkout all files with force...");
			Debug.Log($"[GitService] Using target: {repo.Head.FriendlyName}");
			
			repo.CheckoutPaths(repo.Head.FriendlyName, new[] { "*" }, checkoutOptions);
			Debug.Log($"[GitService] CheckoutPaths completed");
			
			// Alternative: try checking out the head directly
			Debug.Log($"[GitService] Also attempting direct head checkout...");
			repo.Checkout(repo.Head.Tip.Tree, new[] { "*" }, checkoutOptions);
			Debug.Log($"[GitService] Direct head checkout completed");
			
			Debug.Log($"[GitService] Files checked out successfully, verifying results...");
			
			var files = Directory.GetFiles(_repoPath, "*", SearchOption.AllDirectories)
				.Where(f => !f.Contains(".git"))
				.ToArray();
			Debug.Log($"[GitService] Repository now contains {files.Length} non-git files");
			
			if (files.Length > 0)
			{
				Debug.Log($"[GitService] Sample checked out files:");
				for (int i = 0; i < Math.Min(files.Length, 5); i++)
				{
					var relativePath = Path.GetRelativePath(_repoPath, files[i]);
					var fileSize = new FileInfo(files[i]).Length;
					Debug.Log($"[GitService]   - {relativePath} ({fileSize} bytes)");
				}
			}
			
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError($"[GitService] Checkout failed with exception: {ex.GetType().Name}");
			Debug.LogError($"[GitService] Exception message: {ex.Message}");
			Debug.LogError($"[GitService] Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Debug.LogError($"[GitService] Inner exception: {ex.InnerException.Message}");
			}
			return false;
		}
	}
}
