﻿using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using System.Configuration.Install;
using HenIT.Services;

namespace MailAggregator
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			if (Properties.Settings.Default.NewVersion)
			{
				Properties.Settings.Default.Upgrade();
				Properties.Settings.Default.NewVersion = false;
				Properties.Settings.Default.Save();
			}
			if (args.Length > 0)
			{
				if (args[0].ToUpper() == "-INSTALL")
				{
					InstallService();
					return;
				}
				else if (args[0].ToUpper() == "-UNINSTALL")
				{
					UnInstallService();
					return;
				}
			}

			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] 
			{ 
				new MailAggregatorService() 
			};
			ServiceBase.Run(ServicesToRun);
		}

		#region Installing/uninstalling the service
		private static bool InstallService()
		{
			bool success = false;
			try
			{
				string exeFullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				string workingPath = System.IO.Path.GetDirectoryName(exeFullPath);
				string logPath = System.IO.Path.Combine(workingPath, "Install.log");
				ServiceStartMode startmode = ServiceStartMode.Automatic;
				ServiceAccount account = ServiceAccount.LocalService;
				string username = "";
				string password = "";

				InstallerForm installerForm = new InstallerForm();
				installerForm.StartType = ServiceStartMode.Automatic;
				installerForm.AccountType = ServiceAccount.User;
				installerForm.BringToFront();
				installerForm.TopMost = true;
				if (installerForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					startmode = installerForm.StartType;
					account = installerForm.AccountType;
					if (installerForm.AccountType == ServiceAccount.User)
					{
						username = installerForm.UserName;
						password = installerForm.Password;
					}
				}

				Hashtable savedState = new Hashtable();
				ProjectInstaller myProjectInstaller = new ProjectInstaller(true);
				InstallContext myInstallContext = new InstallContext(logPath, new string[] { });
				myProjectInstaller.Context = myInstallContext;
				myProjectInstaller.ServiceName = "Mail Aggregator Service";
				myProjectInstaller.DisplayName = "Mail Aggregator Service";
				myProjectInstaller.Description = "Mail Aggregator Service";
				myProjectInstaller.StartType = startmode;
				myProjectInstaller.Account = account;
				if (account == ServiceAccount.User)
				{
					myProjectInstaller.ServiceUsername = username;
					myProjectInstaller.ServicePassword = password;
				}
				myProjectInstaller.Context.Parameters["AssemblyPath"] = exeFullPath;

				myProjectInstaller.Install(savedState);
				success = true;
			}
			catch (Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(ex.Message, "Install service", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			return success;
		}
		private static bool UnInstallService()
		{
			bool success = false;
			try
			{
				ServiceController sc = new ServiceController("Mail Aggregator Service");
				if (sc == null)
				{
					System.Windows.Forms.MessageBox.Show("Service not installed or accessible!", "Stopping service", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
					return true;
				}
				if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
				{
					sc.Stop();
				}
			}
			catch (Exception ex)
			{
				if (!ex.Message.Contains("was not found on computer"))
				{
					System.Windows.Forms.MessageBox.Show(ex.Message, "Stopping service", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				}
				else
					return true;
			}
			try
			{
				string exeFullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				string workingPath = System.IO.Path.GetDirectoryName(exeFullPath);
				string logPath = System.IO.Path.Combine(workingPath, "Install.log");

				ServiceInstaller myServiceInstaller = new ServiceInstaller();
				InstallContext Context = new InstallContext(logPath, null);
				myServiceInstaller.Context = Context;
				myServiceInstaller.ServiceName = "Mail Aggregator Service";
				myServiceInstaller.Uninstall(null);
				success = true;
			}
			catch (Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(ex.Message, "Uninstall service", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			return success;
		} 
		#endregion
	}
}
