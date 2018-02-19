<Query Kind="Program">
  <Reference>&lt;ProgramFilesX86&gt;\Encompass\Elli.ElliEnum.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Encompass\EllieMae.Encompass.AsmResolver.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Encompass\EllieMae.Encompass.Runtime.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Encompass\EncompassObjects.dll</Reference>
  <Namespace>EllieMae.Encompass.BusinessObjects.Loans</Namespace>
  <Namespace>EllieMae.Encompass.BusinessObjects.Loans.Templates</Namespace>
  <Namespace>EllieMae.Encompass.Client</Namespace>
  <Namespace>EllieMae.Encompass.Collections</Namespace>
  <Namespace>EllieMae.Encompass.Query</Namespace>
  <Namespace>EllieMae.Encompass.Reporting</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
</Query>

void Main()
{
	var Factory = SessionFactory.Production;

	using( var session = Factory.Create()){
		var fields = Factory.SearchFields("CX.");
		//var fields = Factory.SearchReportingFields("CX.");

		fields.OrderBy(f => f.FieldID).Dump();
		//session.ClientID.Dump();
    //session.PrintTemplateHierarchy(TemplateEntry.PublicRoot);
	}
}

//Classes
public static class MyExtensions
{
	public static ObjectIDGenerator idGen = new ObjectIDGenerator();

	public static TResult WrapDisposable<TInput, TResult>(this TInput source, Func<TInput, TResult> activity) where TInput : IDisposable
	{
		bool firstTime;
		long sourceId = idGen.GetId(source, out firstTime);
		Debug.WriteLine("  -- Wrapping {0} {1}", typeof(TInput), sourceId);

		try
		{
			return activity(source);
		}
		finally
		{
			var session = source as Session;
			if (session != null)
			{
				session.End();
				Debug.WriteLine("** Ended session **");
			}
			source.Dispose();
			Debug.WriteLine("  -- Disposed {0} {1}", typeof(TInput), sourceId);
		}
	}

	public static void WrapDisposable<TInput>(this TInput source, Action<TInput> activity)
	where TInput : IDisposable
	{
		bool firstTime;
		long sourceId = idGen.GetId(source, out firstTime);
		Debug.WriteLine("  -- Wrapping {0} {1}", typeof(TInput), sourceId);

		try
		{
			activity(source);
		}
		finally
		{
			source.Dispose();
			Debug.WriteLine("  -- Disposed {0} {1}", typeof(TInput), sourceId);
		}
	}

	public static void Do(this PipelineData data, Action<PipelineData> action)
	{
		action(data);
	}

	public static string GetGuidForLoan(this Session session, string loanNumber)
	{
		var qry = session.Loans.Query(new StringFieldCriterion("Loan.LoanNumber", loanNumber, StringFieldMatchType.Exact, true));
		return qry.OfType<LoanIdentity>().First().Guid;
	}


	public static string GetRoleEmailRecipients(EllieMae.Encompass.BusinessObjects.Loans.Role p_role, EllieMae.Encompass.BusinessObjects.Loans.Loan p_loan)
	{
		//get loan associates with the assigned role
		EllieMae.Encompass.Collections.LoanAssociateList al = p_loan.Associates.GetAssociatesByRole(p_role);
		List<string> recipientList = new List<string>();

		foreach (EllieMae.Encompass.BusinessObjects.Loans.LoanAssociate associate in al)
		{
			if (!string.IsNullOrEmpty(associate.ContactEmail))
			{
				recipientList.Add(associate.ContactEmail);
			}
		}

		return String.Join(",", recipientList);
	}

	// Allows for recursively traversing the template hierarchy
	public static void printTemplateHierarchy(Session session, TemplateEntry parent)
	{
		// Retrieve the contents of the specified parent folder
		TemplateEntryList templateEntries = session.Loans.Templates.GetTemplateFolderContents(TemplateType.LoanTemplate, parent);

		// Iterate over each of the TemplateEntry records, each of which represents either
		// a Template or a subfolder of the parent folder.
		foreach (TemplateEntry e in templateEntries)
		{
			printTemplateEntry(e);

			// If the entry represents a subfolder, recurse into that folder
			if (e.EntryType == TemplateEntryType.Folder)
				printTemplateHierarchy(session, e);
		}
	}

	// Prints the details of a single TemplateEntry object
	private static void printTemplateEntry(TemplateEntry e)
	{
		Console.WriteLine("-> " + e.Name);
		Console.WriteLine("   Type = " + e.EntryType);
		Console.WriteLine("   IsPublic = " + e.IsPublic);
		Console.WriteLine("   LastModified = " + e.LastModified);
		Console.WriteLine("   Owner = " + e.Owner);
		Console.WriteLine("   ParentFolder = " + e.ParentFolder);
		Console.WriteLine("   Path = " + e.Path);
		Console.WriteLine("   RepositoryPath = " + e.DomainPath);

		foreach (string name in e.Properties.GetPropertyNames())
			Console.WriteLine("   Properties[\"" + name + "\"] = " + e.Properties[name]);

		Console.WriteLine();
	}
}

public class SessionFactory
{
	public static SessionFactory Develop = new SessionFactory { DisplayName = "Development", UserId = "YourUserNameHere", Password = Util.GetPassword("develop"), ServerUri = "https://YourServerIdHere.ea.elliemae.net$YourServerIdHere" };
	public static SessionFactory Test = new SessionFactory { DisplayName = "Test", UserId = "YourUserNameHere", Password = Util.GetPassword("test"), ServerUri = "https://YourServerIdHere.ea.elliemae.net$YourServerIdHere" };
	public static SessionFactory Staging = new SessionFactory { DisplayName = "Staging", UserId = "YourUserNameHere", Password = Util.GetPassword("staging"), ServerUri = "https://YourServerIdHere.ea.elliemae.net$YourServerIdHere" };
	public static SessionFactory Production = new SessionFactory { DisplayName = "Production", UserId = "YourUserNameHere", Password = Util.GetPassword("production"), ServerUri = "https://YourServerIdHere.ea.elliemae.net$YourServerIdHere" };

	static SessionFactory()
	{
		var runtime = new EllieMae.Encompass.Runtime.RuntimeServices();
		runtime.Initialize();
	}

	private SessionFactory() { }

	public string DisplayName { get; private set; }
	public string ServerUri { get; private set; }
	public string UserId { get; private set; }
	public string Password { get; private set; }

	public Session Create()
	{
		return Create(null);
	}

	public Session Create(string nickName)
	{
		var session = new Session();

		if (string.IsNullOrEmpty(nickName))
		{
			Debug.WriteLine(string.Format("** Starting session on {0} as {1} **", DisplayName, UserId));
		}
		else
		{
			Debug.WriteLine(string.Format("** Starting {0} session on {1} as {2} **", nickName, DisplayName, UserId));
		}

		session.Start(ServerUri, UserId, Password);
		session.Disconnected += Session_Disconnected;

		return session;
	}

	private void Session_Disconnected(object sender, DisconnectedEventArgs e)
	{
		Debug.WriteLine(string.Format("** Session disconnected from {0} **", DisplayName));
	}

	public TResult Do<TResult>(Func<Session, TResult> activity)
	{
		return Create().WrapDisposable(activity);
	}

	public void Do(Action<Session> activity)
	{
		Create().WrapDisposable(activity);
	}

	public IEnumerable<FieldDescriptor> GetFields(params string[] fieldIDs)
	{
		return Do(session =>
			   session.Loans.FieldDescriptors.StandardFields.OfType<FieldDescriptor>()
					  .Union(session.Loans.FieldDescriptors.CustomFields.OfType<FieldDescriptor>())
					  .Union(session.Loans.FieldDescriptors.VirtualFields.OfType<FieldDescriptor>())
					 .Where(f => fieldIDs.Length == 0 || fieldIDs.Contains(f.FieldID)));
	}

	public IEnumerable<FieldDescriptor> SearchFields(params string[] keywords)
	{
		return Do(session =>
			   session.Loans.FieldDescriptors.StandardFields.OfType<FieldDescriptor>()
					  .Union(session.Loans.FieldDescriptors.CustomFields.OfType<FieldDescriptor>())
					  .Union(session.Loans.FieldDescriptors.VirtualFields.OfType<FieldDescriptor>())
					 .Where(f => keywords.Any(kw =>
							f.Description.IndexOf(kw, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
							f.FieldID.IndexOf(kw, StringComparison.CurrentCultureIgnoreCase) >= 0)));
	}

	public IEnumerable<ReportingFieldDescriptor> GetReportingFields(params string[] fieldIDs)
	{
		return Do(session =>
			   session.Reports.GetReportingDatabaseFields().OfType<ReportingFieldDescriptor>()
			   .Where(d => fieldIDs.Length == 0 || fieldIDs.Contains(d.FieldID)));
	}

	public IEnumerable<ReportingFieldDescriptor> SearchReportingFields(params string[] keywords)
	{
		return Do(session =>
			   session.Reports.GetReportingDatabaseFields().OfType<ReportingFieldDescriptor>()
			   .Where(d => keywords.Any(kw =>
					 d.FieldID.IndexOf(kw, StringComparison.CurrentCultureIgnoreCase) >= 0
					 || d.Description.IndexOf(kw, StringComparison.CurrentCultureIgnoreCase) >= 0)));
	}
}

public static class DbExtensions
{
	/// <summary>
	///     Gets and converts the value from the specified field.
	/// </summary>
	/// <typeparam name="T">The type of value to return.</typeparam>
	/// <param name="reader">The IDataReader to read the value from.</param>
	/// <param name="fieldName">The name of the field containing the value.</param>
	/// <returns>
	///     The value contained in the field, if the field contains a non-null value;
	///     otherwise, the <c>default</c> value for <typeparamref name="T"/>.
	/// </returns>
	public static T GetValueOrDefault<T>(this IDataReader reader, string fieldName)
	{
		return (T)(Convert.IsDBNull(reader[fieldName]) ? default(T) : reader[fieldName]);
	}

	public static void DumpAsInsert<T>(this IEnumerable<T> data) where T : class
	{
		DumpAsInsert(data, null);
	}

	public static void DumpAsInsert<T>(this IEnumerable<T> data, string tableName) where T : class
	{
		DumpAsInsert(data, tableName, string.Empty);
	}

	public static void DumpAsInsert<T>(this IEnumerable<T> data, string tableName, string hideColumn) where T : class
	{
		DumpAsInsert(data, tableName, new string[] { hideColumn });
	}

	public static void DumpAsInsert<T>(this IEnumerable<T> data, string tableName, string[] hideColumns) where T : class
	{
		var firstItem = data.FirstOrDefault();
		if (firstItem == null) string.Empty.Dump();
		if (hideColumns == null) hideColumns = new[] { string.Empty };

		if (tableName == null)
			tableName = firstItem.GetType().Name;

		var formatProvider = GetSqlTextFormatInfo();
		var result = new StringBuilder();
		var members = new List<MemberInfo>();
		if (IsAnonymousType(firstItem.GetType()))
			members.AddRange(firstItem.GetType().GetProperties().Where(p => !hideColumns.Contains(p.Name)));
		else
			members.AddRange(firstItem.GetType().GetFields().Where(p => !hideColumns.Contains(p.Name)));

		var stmt = string.Format("INSERT INTO [{0}] ({1})\nVALUES (", tableName, string.Join(", ", members.Select(p => string.Format("[{0}]", p.Name)).ToArray()));

		foreach (var item in data)
		{
			result.Append(stmt);

			var first = true;
			foreach (var col in members)
			{
				if (!first) result.Append(",");
				first = false;
				result.Append(GetFieldValue(formatProvider, col, item));
			}
			result.AppendLine(");");
		}

		result.ToString().Dump();
	}

	public static string GetFieldValue(IFormatProvider formatProvider, MemberInfo field, object row)
	{
		object value;
		Type fieldType;
		if (field is FieldInfo)
		{
			value = ((FieldInfo)field).GetValue(row);
			fieldType = ((FieldInfo)field).FieldType;
		}
		else
		{
			value = ((PropertyInfo)field).GetValue(row, null);
			fieldType = ((PropertyInfo)field).PropertyType;
		}
		if (value == null) return "NULL";

		if (fieldType == typeof(bool))
			return (bool)value ? "1" : "0";

		if (fieldType == typeof(System.String))
			return "'" + value.ToString().Replace("'", "''") + "'";
		else if (fieldType == typeof(DateTime) || fieldType == typeof(DateTime?))
			return "convert(datetime, '" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ssss.fffffff") + "', 120)";
		else if (fieldType == typeof(System.Data.Linq.Binary))
			return "NULL";
		else if (fieldType == typeof(XElement))
			return "'" + ((XElement)value).Value.Replace("'", "''") + "'";
		else
			return string.Format(formatProvider, "{0}", value);
	}

	private static System.Globalization.NumberFormatInfo GetSqlTextFormatInfo()
	{
		return new System.Globalization.NumberFormatInfo()
		{
			CurrencyDecimalSeparator = ".",
			CurrencyGroupSeparator = string.Empty,
			NumberDecimalSeparator = ".",
			NumberGroupSeparator = string.Empty,
			PercentDecimalSeparator = ".",
			PercentGroupSeparator = string.Empty,
		};
	}


	private static bool IsAnonymousType(Type type)
	{
		if (type == null)
			throw new ArgumentNullException("type");

		// HACK: The only way to detect anonymous types right now.
		return Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
			&& type.IsGenericType && type.Name.Contains("AnonymousType")
			&& (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
			&& (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
	}
}

public class EncompassEventBase { }

public class ExceptionMonitorEvent : SessionInformationEventBase
{
	public Exception Exception;

	public ExceptionMonitorEvent(ExceptionMonitorEventArgs args) : base()
	{
		Exception = args.Exception;
		var sessionInfoProp = Exception.GetType().GetProperties().FirstOrDefault(pi => pi.PropertyType == typeof(SessionInformation));
		if (sessionInfoProp != null)
		{
			_sessionInformation = sessionInfoProp.GetValue(Exception) as SessionInformation;
		}
	}
}

public class ConnectionMonitorEvent : EncompassEventBase
{
	public ConnectionMonitorEventType EventType;
	public string ClientIpAddress;

	public ConnectionMonitorEvent(ConnectionMonitorEventArgs args)
	{
		EventType = args.EventType;
		ClientIpAddress = args.ClientIPAddress;
	}
}

public class SessionInformationEventBase : EncompassEventBase
{
	protected SessionInformation _sessionInformation;

	public SessionInformationEventBase() { }

	public SessionInformationEventBase(SessionInformation sessionInfo) : this()
	{
		_sessionInformation = sessionInfo;
	}

	public string ClientHostname { get { return _sessionInformation != null ? _sessionInformation.ClientHostname : null; } }
	public string ClientIPAddress { get { return _sessionInformation != null ? _sessionInformation.ClientIPAddress : null; } }
	public DateTime? LoginTime { get { return _sessionInformation != null ? _sessionInformation.LoginTime : (DateTime?)null; } }
	public string SessionID { get { return _sessionInformation != null ? _sessionInformation.SessionID : null; } }
	public string UserID { get { return _sessionInformation != null ? _sessionInformation.UserID : null; } }
}

public class SessionMonitorEvent : SessionInformationEventBase
{
	public SessionMonitorEventType EventType;

	public SessionMonitorEvent(SessionMonitorEventArgs args) : base(args.SessionInformation)
	{
		EventType = args.EventType;
	}
}

public class LoanMonitorEvent : SessionInformationEventBase
{
	public LoanMonitorEventType EventType;
	public string Guid;
	public string LoanFolder;
	public string LoanName;

	public LoanMonitorEvent(LoanMonitorEventArgs args) : base(args.SessionInformation)
	{
		EventType = args.EventType;
		Guid = args.LoanIdentity.Guid;
		LoanFolder = args.LoanIdentity.LoanFolder;
		LoanName = args.LoanIdentity.LoanName;
	}
}

public class DataExchangeEvent : SessionInformationEventBase
{
	public object Data;

	public DataExchangeEvent(DataExchangeEventArgs args) : base(args.Source)
	{
		Data = args.Data;
	}
}

public static class SessionExtensions
{
	public static string GetGuidFromLoanNumber(this Session session, string loanNumber)
	{
		var qry = session.Loans.Query(new StringFieldCriterion("Loan.LoanNumber", loanNumber, StringFieldMatchType.Exact, true));
		if (!qry.OfType<LoanIdentity>().Any())
		{
			throw new ArgumentException($"Loan Number does not exist in {session.ServerURI}");
		}
		return qry.OfType<LoanIdentity>().First().Guid;
	}

	public static IEnumerable<LoanIdentity> GetGuidsFromLoanNumber(this Session session, string loanNumber)
	{
		var qry = session.Loans.Query(new StringFieldCriterion("Loan.LoanNumber", loanNumber, StringFieldMatchType.Exact, true));
		if (!qry.OfType<LoanIdentity>().Any())
		{
			throw new ArgumentException($"Loan Number does not exist in {session.ServerURI}");
		}
		return qry.OfType<LoanIdentity>().ToArray();
	}

	// Allows for recursively traversing the template hierarchy
	public static void PrintTemplateHierarchy(this Session session, TemplateEntry parent, TemplateType type = TemplateType.LoanTemplate)
	{
		// Retrieve the contents of the specified parent folder
		var templateEntries = session.Loans.Templates.GetTemplateFolderContents(type, parent).OfType<TemplateEntry>();

		// Iterate over each of the TemplateEntry records, each of which represents either
		// a Template or a subfolder of the parent folder.
		foreach (var e in templateEntries)
		{
			PrintTemplateEntry(e);

			// If the entry represents a subfolder, recurse into that folder
			if (e.EntryType == TemplateEntryType.Folder)
				session.PrintTemplateHierarchy(e);
		}
	}

	// Prints the details of a single TemplateEntry object
	private static void PrintTemplateEntry(TemplateEntry e)
	{
		/*
		Console.WriteLine("-> " + e.Name);
		Console.WriteLine("   Type = " + e.EntryType);
		Console.WriteLine("   IsPublic = " + e.IsPublic);
		Console.WriteLine("   LastModified = " + e.LastModified);
		Console.WriteLine("   Owner = " + e.Owner);
		Console.WriteLine("   ParentFolder = " + e.ParentFolder);
		Console.WriteLine("   Path = " + e.Path);
		Console.WriteLine("   RepositoryPath = " + e.DomainPath);

		foreach (string name in e.Properties.GetPropertyNames())
			Console.WriteLine("   Properties[\"" + name + "\"] = " + e.Properties[name]);

		Console.WriteLine();
		*/
		e.Dump();
	}

	public static bool IsProduction(this Session session)
	{
		return session.ServerURI.Contains("YourServerIdHere");

	}

	public static bool IsStaging(this Session session)
	{
		return session.ServerURI.Contains("YourServerIdHere");
	}

	public static bool IsTesting(this Session session)
	{
		return session.ServerURI.Contains("YourServerIdHere");
	}

	public static bool IsDevelop(this Session session)
	{
		return session.ServerURI.Contains("YourServerIdHere");
	}

	public static bool IsSandbox(this Session session)
	{
		return !(session.IsProduction() || session.IsStaging() || session.IsTesting() || session.IsDevelop());
	}
}
