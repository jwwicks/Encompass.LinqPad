<Query Kind="Program">
  <Namespace>EllieMae.Encompass.BusinessObjects.Users</Namespace>
</Query>

void Main()
{
  /* dump all users but excluded below */
	var excluded = new[] {
		"admin",
    "training"
	};

	using(var session = SessionFactory.Production.Create()){
		var orgs = session.Organizations.GetAllOrganizations().OfType<Organization>()
			.Select(o => new {o.ID, o.OrgCode, o.Description, o.IsTopMostOrganization});

		var allUsers = session.Users.GetAllUsers().OfType<User>()
      //Also ignore users that start with test or end with user - typical service only account names YMMV
			.Where(u => !((u.ID.EndsWith("user") || u.ID.StartsWith("test") || excluded.Contains(u.ID)) && u.Enabled)
			.Join(orgs, user => user.OrganizationID, org => org.ID, (user, org) => new {user.ID, user.FullName, org.OrgCode, org.Description})
			.OrderBy(u => u.FullName)
			.Select(u => u);

		allUsers.Dump();
	}
}
