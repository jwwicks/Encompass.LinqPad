<Query Kind="Program">
  <NuGetReference>LinqToExcel</NuGetReference>
  <Namespace>EllieMae.Encompass.BusinessObjects.Users</Namespace>
  <Namespace>LinqToExcel</Namespace>
  <Namespace>LinqToExcel.Attributes</Namespace>
</Query>

void Main()
{
  /* Often times we needed to disable production users that had been replicated to other
     environments. Doing this by hand is tedious and error prone. You also typically need
     to exclude some users that have to use these other environments. That's where this script comes in
  */

  /* create a list of normally excluded users, developers testers and admins etc... */
  string location = "C:\\Projects\\Encompass.LinqPad\\Queries\\ExcludedUsers.xlsx";

  /* Warning running this in production is not recommended unless disabling folks in production is what you want */
  var Factory = SessionFactory.Production;

	var excel = new ExcelQueryFactory(location);
	excel.DatabaseEngine = LinqToExcel.Domain.DatabaseEngine.Ace;

	var excluded = (
			from u in excel.Worksheet<MyUser>("Table")
			select u.ID)
		.ToList();

  /* Uncomment the line below and set a break point (first run)
     to test you are getting the all the excluded users
  */
  //excluded.Dump();

	using (var session = Factory.Create())
	{

		"Executing!".Dump();
		var allUsers = session.Users.GetAllUsers().OfType<User>()
			.Where(u => !excluded.Contains(u.ID) && u.Enabled);

    /* Uncomment the line below and set a break point (first run)
       to make sure you don't disable someone incorrectly
    */
		//allUsers.Dump();

    /* no turning back from here */
		foreach (User u in allUsers)
		{
			u.FullName.Dump(u.ID);

			var name = u.ID;
			u.Disable();
			u.AccountLocked = true;
			u.Commit();
			u.FullName.Dump("Disabled and locked");
		}
	}

}

/**
  Each field in this class should match up with a column
  dumped from Encompass user list. Rename the columns in the Excel sheet
  or use the LinqToExcel mapping attributes. [ExcelColumn("UserId")] etc...
  https://github.com/paulyoder/LinqToExcel
*/
public class MyUser
{
	public string ID { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string Personas { get; set; }
	public string OrganizationGroup { get; set; }
	public string LastLogin { get; set; }
	public string Login { get; set; }
	public string Account { get; set; }
	public string Email { get; set; }
	public string Phone { get; set;}
}
