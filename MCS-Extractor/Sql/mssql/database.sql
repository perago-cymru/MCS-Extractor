/**
Database definition. Create your database for the MCS Extractor and then run this script to create tables and functions. 
Then users should be able to sign in directly with a connection string. Note that users will need to have permissions to create tables and 
functions on this database as that is part of data import.
**/
CREATE TABLE csv_index_fields
(
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    table_name varchar(50) NOT NULL,
    index_field varchar(50) ,
    start_field varchar(50) ,
    close_field varchar(50) ,
    unique_identifier varchar(255) 
);

CREATE TABLE  csv_table_mappings
(
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    csv_name varchar(255)  NOT NULL,
    db_name varchar(255)  NOT NULL,
    table_name varchar(75)  NOT NULL,
    type_name varchar(50)  NOT NULL
);

CREATE TABLE  loaded_files
(
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    loaded datetime NOT NULL DEFAULT GETDATE(),
    filename varchar(255) NOT NULL
);

GO

CREATE OR ALTER FUNCTION fiscal_quarter(@dt datetime)
    RETURNS varchar(12)
AS
BEGIN
	DECLARE @dateYear INT,
	 @quarter INT,
	 @startYear INT,
	 @endYear INT;

	  SET @dateYear = DATEPART( YEAR, @dt );
	  SET @quarter = DATEPART(Q, @dt);
	  SET @startYear = @dateYear
	  SET @endYear = @dateYear+1
	  IF ( @quarter = 1 ) BEGIN
		 SET @startYear = @startYear-1
		 SET @endYear = @dateYear
	  END
	  SET @quarter = @quarter-1
	  if ( @quarter = 0 )
   		SET @quarter = 4;
	  RETURN CONCAT(@startYear, '-', @endYear, '-Q', @quarter);
END;
GO

CREATE OR ALTER FUNCTION fiscal_year(@dt datetime)
    RETURNS varchar(9)
AS
BEGIN
declare @dateYear INT;
declare @quarter INT;
declare @startYear INT;
declare @endYear INT;

  SET @dateYear = YEAR( @dt );
  SET @quarter = DATEPART(q, @dt);
  SET @startYear = @dateYear;
  SET @endYear = @dateYear+1;
  IF ( @quarter = 1 ) BEGIN
     SET @startYear = @startYear-1;
	 SET @endYear = @dateYear;
  END

  RETURN CONCAT(@startYear, '-', @endYear);
END;

GO

