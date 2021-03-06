# The MCS Data Extractor

This tool is designed to facilitate importing data exported in CSV format from the MyCouncilServices platform. The tool is built to be a lightweight shell to import CSV data into PostgreSQL. It maintains itself through configuration, largely stored in the database. The project is released under an MIT license.

## Installation 

The MCS Data Extractor has the following pre-requisites:

* Microsoft .Net Runtime (the initial release is built against version 4.7.1) any up-to-date Windows system should be more up to date.

### Postgres version
* PostgreSQL database ( https://www.postgresql.org/ )
* Postgres ODBC Connector ( https://odbc.postgresql.org/ )


The Bootstrapper installer aims to install these pre-requisites and the MCS Extractor tool, but they can be installed manually. Additionally the tool itself has no registry dependencies or other installation elements, as long as the `Downloaded` and `SQL` folders are retained, it should be quite safe to move it into different folders.

On the first run the tool will request the username and password for your Postgres installation. It will use this to create the database and configure the relevant connection strings. As standard, the database will be named `my_council_services_extract` and this can be changed in the application.config file if necessary. On first run this database is created along with some standard functions from the `/MCS-Extractor/Sql/database.sql` folder - these are detailed below.

Having created the database, the tool creates an ODBC connection to facilitate access from office tools. 


### Microsoft SQL Server version
* Microsoft SQL Server instance available to the system performing the install.

The installer will not be able to install SQL Server, this version anticipates that the database infrastructure is already present. If you are using SQL Server as your database back-end you do not need the bootstrapper.

Prior to running the MCS Extractor you will need to create the database from `Sql/mssql/database.sql`, give the user who will be running the tool access rights including creating tables and user functions. You will need to provide a ConnectionString to your user at first run for them to be able to access the database.

## Use 

On first run the MCS Data Extractor will ask which database you are using and ask for some connection parameters- a connection string for SQL server, database name and user credentials for Postgres. This information will be stored in your user's roaming profile in `%USER%\AppData\Roaming\MCS Extractor` - if the connection is failing you can change the values in this folder with a text editor (it is a standard XML config file) or delete the folder to force the application to recreate it.

Having got the connection established it will look for the tables it depends on and create them if it cannot find them. 

When you run an import, the MCS Data Extractor searches the `/Downloaded` folder of the application installation directory for .csv files. When it finds one, it goes through the following steps:

* Read file from folder.
* Check whether the file has already been seen and ignore it if it has.
* If the file is new, read in the contents and see whether an existing mapping exists.
* If a mapping exists, import the contents of the file to the data store.
* If no mapping exists, show the mapping menu. Once a mapping has been created, import the contents of the file.

It will go through every file in the folder each run, so once data has been imported it will save a little time to remove it from the folder. If the import fails it will raise a message box to notify of this, which should offer enough information to adjust mappings by hand if necessary.

Not that the application uses filepaths only to judge whether or not it has seen a file before, moving files to a different folder or having the same files imported from multiple computers could all result in duplicated data being imported. This is one reason that uniqueness checks on the MCS Id field are useful. The other reason is that the same record can be created when the system is updated- for example when a case is closed a new row will be added. Currently the importer does not update existing rows so it will definitely create duplicates.

### Creating a new mapping 

There are five things that the MCS Data Extractor needs a record to have:
* A table name - this should be the name for the imported data table. The table must have a unique name, and should be self-explanatory, so you can recognise what you want to query. The table name *should have no spaces* so rather than "recycling equipment requests" try "recycling_equipment_requests" or similar.
* The name of the My Council Services request id field. This is often "servicerequest."
* A start date - the date the record was created.
* A close date - the date the record is closed.
* A user identifying field - This is a little more tricky to identify because MCS does not provide any data that can identify a specific user. This can be a combination of fields (the first line of the address and postcode, for example) or a single field such as the UPRN.

When a new mapping is being created, the system will load in the the first fifty records (or as many are available in the csv file) and then offer those in a table with their CSV field names and a dropdown to choose the type of each field. The system will try to guess as well as it can, but it won't always get things right so the table will show the first three records as examples of the type of data. The mapping needs the largest/highest/lowest value for the field to accurately choose the type - if the system has recommended a larger field type it is wise to trust its guidance as it will have run through a number of rows. Changing a 'Text' to a 'String' is likely to result in a mapping that fails to import, but changing 'String' to 'Text' will result in a table that takes up more space in storage but will have space for a lot of text.

The options are:
* Boolean - either "true" or "false"
* String - a short piece of text, no more than 255 characters long. Most fields end up as a string.
* Text - any text more than 255 characters - if a field gets multi-line responses, it is probably a text field.
* Integer - A whole number, positive or negative, with no decimal place. The range of integers is from -2147483648 to +2147483647. This will be treated as numeric so for data where leading zeroes matter, such as, phone numbers use a String instead.
* Long - A whole number larger than an integer.
* Double - A variable-precision floating point number.
* Decimal - An exact floating point number.
* Date - A Date and/or time field.

Once field types have been selected for every field and the start, close and identifying fields are chosen (start and close should both be date fields) the "Create Table" button will create the table.

### Resolving type errors 

It is possible that import will fail with an error caused by a failed type mapping. The most likely cause is where a field mapped as a string gets a value that is too large. This can be resolved with a little manual adjustment of the database, for which you will need PGAdmin, the standard posgres administration tool, which is installed with Postgres as standard or SQL Server Management Studio, depending on which you are using.

The process is as follows:

#### Postgres

1. Open PGAdmin and select 'localhost' from the 'servers' list.
2. Open your database ('my_council_services_extract') from the database.
3. Open the table you are importing to- this will be in `Schemas > public > Tables > [your table name]`
4. Open the `Columns` list and right click on the column where the problem exists, select `properties`.
5. Go to the `Definition` tab and change the `data type` to the new type. If you have a problem with over-long text, the field will probably be of type 'character varying' (which corresponds to 'string' in the import tool) and you will need to change it to `text`. Not all field types will be available because changing between a numeric and textual data type can't be managed automatically.
7. At the top of the view, select the `query` tool (resembling a triple-stacked cake with a play button by it) and then in the Query Editor type `SELECT * FROM csv_table_mappings WHERE table_name = '[your table name]'` to find all the mappings for your table.
8. Double click on the `type_name` column in the row where the `db_name` matches the field you just changed and change it to the new type, which must be one of `Boolean, Varchar, Integer, Bigint, text, Double, Numeric, Date` - these correspond to the options above, `Varchar` is the same as `String`, `Bigint` is `Long` and `Numeric` is `Decimal`. If you have any other fields of the same type, copy the value from there. Click on "save data changes" at the top (three stacked cakes with a padlock) to save the change. 
9. Hit the 'OK' button and the change will be saved.

With that done, you should be able to re-run your import.

#### SQL Server

SQL Server won't allow you to update a column, so if this happens a while into your run you will need to copy the table, move the records across from it and then update all the references to it.

1. Open The SQL Server Management Studio and connect to your local server.
2. Open your MCS Extractor database (this will be named in the ConnectionString) from the "Databases" folder.
3. Right click on your data table and select "Script Table as > CREATE TO"
4. In the provided query window provide a new name for the table and rename the primary key in the `CONSTRAINT [PK__{tablename}]` section as the existing one will already exist.
5. Find the field that is of the incorrect type and update it, then click the `! Execute` button to run the query.
6. Clear the creation statement or open a new query window, you will need every field name from the table and then you can create a `INSERT/SELECT` query along these lines: `INSERT INTO my_new_table(servicerequest, status, name, reported_on, reported_by, closed_on, incident_id, USRN, UPRN, ... ) SELECT (servicerequest, status, name, reported_on, reported_by, closed_on, incident_id, USRN, UPRN, ... ) FROM my_old_table` - note that if there is a change from `nvarchar` to `int` or similar you will have to include a type conversion into your `SELECT` query but if you're using the SQL Server version this should be easy for you.
7. Update your mappings table: in the query window use `UPDATE csv_table_mappings SET table_name='my_new_table' WHERE table_name='my_old_table' and click `!Execute`.
8. Update your index field table: in the query window use `UPDATE csv_index_fields SET table_name='my_new_table' WHERE table_name='my_old_table' and click `!Execute.`
9. That should be enought to get it working - once you have validated that the correct data is in place you can safely `DROP` your old table. 

If you are on your initial import it may be quicker to simply blitz everything and start again: 

```
truncate table csv_table_mappings;
truncate table loaded_files;
truncate table csv_index_fields;
drop table my_old_table;
```
Note that this will blitz _everything_  - you can use more specific queries similar to those described above in steps 7 and 8 to delete with more care, but clearing `loaded_files` may call for more discretion.

## Accessing Imported Data in Postgres

The data can always be queried directly through the PGAdmin SQL tool, but for practicality the Postgres ODBC connector makes it available through the standard ODBC interface. This can be consumed from many different applications, but the most common one is likely to be Excel.

### Consuming ODBC Data In Excel
1. In your Excel document select `Data` and then on the left of the bar click the `Get Data` button.
2. On the menu select `From other sources` and then `ODBC`.
3. On the ODBC menu that opens select `MCS-Extractor` as your DSN.
4. Finally open the `Advanced Options` section and add an SQL statement to choose the data you are importing. The simplest SQL statement would be `select * from [your table name]` but you can query this data in any configuration that you find useful and there are several built in views to help with this.

## Accessing Imported Data in SQL Server

If you are using SQL Server you can always query the data from the SQL Server enterprise manager, but SQL Server is very well integrated with Microsoft's Office platform.

In Excel querying SQL Server is as simple as going to the `Data` tab on the Ribbon, clicking `Get Data` and then selecting `SQL Server` which will ask for your server and database name. Open the `advanced` section to query one of the views below.

## Existing Views
When a new mapping table is created, some views are created along with it, to provide some easy access to data. These are the built-in views:

### [table]_delivery_periods

Query: `SELECT * FROM [your table name]_delivery_periods`

This view evaluates requests closed per month over the timespan that has been imported and examines them in terms of the duration between the start and close dates of those requests. It shows how many were closed within two weeks, in two to four weeks and over six weeks, along with the average and median number and the average number of requests closed per day.

The view ignores requests where the start and close dates are the same because these are likely to be special cases and not informative with regard to the standard delivery process.

### [table]_quarterly_durations

Query: `SELECT * FROM [your table name]_quarterly_durations`

This view queries the number of requests closed by quarter, providing the number of requests, the number of unique users and duplicate requests (as far as this can be estimated from the data) along with the the mean and median turnaround for those requests. 


### [table]_duplicates

Query: `SELECT * FROM [your table name]_duplicates`

  This view only returns a list of request ids with the number of duplicates that they super-useful because gives an authentic list of duplicates based on the MCS id - the data is likely to contain duplicates because the reporting tool does not guarantee uniqueness, but the ServiceRequest should be unique. Although the results of the query are not very interesting on their own, they open the door to some useful opportunities by joining them to other queries. For example:
 ```
  SELECT duplicates.request_set, req.*, duplicates.duplicates FROM recycling_requests req 
   INNER JOIN recycling_requests_duplicates duplicates 
   ON req.servicerequest = duplicates.servicerequest
   WHERE req.status='Under Review'
   ORDER BY duplicates.duplicates desc, req.servicerequest;
```
This query will show you all currently "Under Review" records side by side with any records that have been identified as duplicates. 

## Limitations 

### Changes to data structure 
The MCS Extractor tool recognises table mappings based on the headers of the CSV file it is importing. If that CSV structure changes it will identify a new table. For example if a new question was added in My Council Services so that instead of ending at `Question 10` and `Answer 10` the CSV now contained `Question 11` and `Answer 11`, the system would identify this as a different table. 

It is possible to work around this by updating the mappings table by hand in a similar way to that described above, but creating a new field in the table and a new mapping in order to facilitate the change. Bear in mind that with this done, it may not recognise older CSV data that ended at `Question 10` as belonging to the same table.

### Matching up different, correlated data 
The MCS Extractor tool is fundamentally simple and does not have the ability to automatically identify related data- for example the MCS quarterly and weekly reports both cover the same cases but provide somewhat different data about them. In general the weekly reports are more detailed and useful _but_ it may be useful to relate them both together. This is done fairly easily using SQL - unique cases will be identified from the `ServiceRequest` field, which is the Unique MCS Id for a given request, so this can be used for `JOIN` statements to combine that data.





