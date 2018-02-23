<Query Kind="Program">
  <Reference>C:\SmartClientCache\Apps\UAC\Ellie Mae\xIHR5EqGa7zPnRG0YpD5z4TPAB0=\Encompass360\ClientServer.dll</Reference>
  <Reference>C:\SmartClientCache\Apps\UAC\Ellie Mae\xIHR5EqGa7zPnRG0YpD5z4TPAB0=\Encompass360\ClientSession.dll</Reference>
  <Reference>C:\SmartClientCache\Apps\UAC\Ellie Mae\xIHR5EqGa7zPnRG0YpD5z4TPAB0=\Encompass360\EMCommon.dll</Reference>
  <Reference>C:\SmartClientCache\Apps\UAC\Ellie Mae\xIHR5EqGa7zPnRG0YpD5z4TPAB0=\Encompass360\EncompassAutomation.dll</Reference>
  <Namespace>EllieMae.EMLite.RemotingServices</Namespace>
  <Namespace>EllieMae.Encompass.Forms</Namespace>
  <Namespace>EllieMae.EMLite.ClientServer</Namespace>
</Query>

void Main()
{
	var repositoryBase = @"C:\Projects\MyBaseSolutionDirectory";
	var releaseType = "Debug";

	var pluginList = new PluginDeploymentDTO[] {
		new PluginDeploymentDTO() {File="MyPlugin.dll", Path="MyPluginDirectory"},
		new PluginDeploymentDTO() {File="SomeOtherPlugin.dll", Path="SomeOtherPluginDirectory"},
		//new PluginDeploymentDTO() {File="", Path=""},
	};

	var formList = new FormDeploymentDTO[]{
		new FormDeploymentDTO(){File="My.EncompassCustomForms.dll", Path=@"EncompassCustomFormsDirectory", Name="EncompassCustomForms"},
		//new FormDeploymentDTO(){File="", Path="", Name=""},
	};

	var Factory = SessionFactory.Production;
	EllieMae.EMLite.RemotingServices.Session.Start(Factory.ServerUri, Factory.UserId, Factory.Password, "FormEditor");

	foreach (var deployable in pluginList.OfType<IDeployable>())
	{
		deployable.Save($@"{repositoryBase}\MyEncompassPlugins\{deployable.Path}\bin\{releaseType}\{deployable.File}");
	}

	foreach (var deployable in formList.OfType<IDeployable>())
	{
		deployable.Save($@"{repositoryBase}\MyEncompassCustomForms\{deployable.Path}\bin\{releaseType}\{deployable.File}");
	}

	EllieMae.EMLite.RemotingServices.Session.End();
}

public interface IDeployable
{
	string Name { get; set; }
	string File { get; set; }
	string Path { get; set; }

	void Save(string path);
}

public class PluginDeploymentDTO : IDeployable
{
	public string Name { get; set; }
	public string File { get; set; }
	public string Path { get; set; }


	public void Save(string path)
	{
		string fileName = System.IO.Path.GetFileName(path);
		EllieMae.EMLite.RemotingServices.Session.ConfigurationManager.InstallPlugin(fileName, new BinaryObject(Utility.GetBinaryFile(path)));

		String.Format($"Plugin: {fileName} uploaded").Dump();
	}
}

public class FormDeploymentDTO : IDeployable
{
	public string Name { get; set; }
	public string File { get; set; }
	public string Path { get; set; }

	public void Save(string path)
	{
		string fileName = System.IO.Path.GetFileName(path);

		Assembly assembly = Assembly.Load(Utility.GetBinaryFile(path));

		var version = new Version(FileVersionInfo.GetVersionInfo(path).FileVersion).ToString();
		var codeBase = new CodeBase(path, assembly.GetName().Name, version, Name);
		EllieMae.EMLite.RemotingServices.Session.FormManager.SaveCustomFormAssembly(codeBase.AssemblyName, new BinaryObject(codeBase.AssemblyPath));

		String.Format($"Form: {Name} uploaded, version: {version}").Dump();
	}
}


public static class Utility
{
	public static void SaveCustomDataObject(string path)
	{
		string fileName = System.IO.Path.GetFileName(path);

		Session.ConfigurationManager.SaveCustomDataObject(fileName, new BinaryObject(GetBinaryFile(path)));

		String.Format($"SaveCustomDataObject: {fileName} uploaded").Dump();
	}

	public static byte[] GetBinaryFile(string path)
	{
		byte[] file = null;
		int bufferSize = 1024;
		using (FileStream fileStream = System.IO.File.Open(path, FileMode.Open))
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				byte[] buffer = new byte[bufferSize];
				int readBytesCount = 0;
				while ((readBytesCount = fileStream.Read(buffer, 0, bufferSize)) > 0)
					memoryStream.Write(buffer, 0, readBytesCount);
				file = memoryStream.ToArray();
			}
		}
		return file;
	}
}
