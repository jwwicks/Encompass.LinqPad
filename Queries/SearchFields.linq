<Query Kind="Program">
  <Namespace>EllieMae.Encompass.BusinessObjects.Loans</Namespace>
</Query>

void Main()
{
	/* Searches all Encompass Custom fields CX.XXXX */
	var fields = new[] {"CX."};
  /* change this to query different servers */
	var Factory = SessionFactory.Production;

	var found = Factory.SearchFields(fields).OfType<FieldDescriptor>()
	/* Search the reporting Db for these fields instead
	var found = Factory.SearchReportingFields(fields).OfType<ReportingFieldDescriptor>()
	*/
		/* that have Appraisal in the FieldlID */
		.Where(f => f.FieldID.Contains("Appraisal".ToUpper()))
		/* order the results by FieldId */
		.OrderBy(f => f.FieldID)
		/* Filter the columns displayed */
		.Select(f => new {
			FieldId = f.FieldID,
			Format = f.Format,
			Description = f.Description,
			Options = f.Options
			})
    /* Filter for Reporting Field columns
    .Select(f => new
  		{
  			FieldId = f.FieldID,
  			Format = f.FieldType,
  			Name = f.CanonicalName,
  			Description = f.Description,
  			Table = f.TableName,
  			Column = f.ColumnName
  	})
    */
		.Dump();

}
