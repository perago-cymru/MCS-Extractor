
CREATE TABLE IF NOT EXISTS csv_index_fields
(
    id SERIAL,
    table_name character varying(50) NOT NULL,
    index_field character varying(50) ,
    start_field character varying(50) ,
    close_field character varying(50) ,
    unique_identifier character varying(255) ,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS csv_table_mappings
(
    id SERIAL,
    csv_name character varying(255)  NOT NULL,
    db_name character varying(255)  NOT NULL,
    table_name character varying(75)  NOT NULL,
    type_name character varying(50)  NOT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS loaded_files
(
    id SERIAL,
    loaded timestamp without time zone NOT NULL DEFAULT now(),
    filename character varying(255) NOT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS table_metadata
(
    id SERIAL,
    user_facing_title character varying(255)  NOT NULL,
    table_name character varying(65) NOT NULL,
     PRIMARY KEY (id)
);

CREATE OR REPLACE FUNCTION public.fiscal_quarter(dt timestamp without time zone)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
declare 
   dateYear INT;
   quarter INT;
   startYear INT;
   endYear INT;
begin
  dateYear := EXTRACT( YEAR from dt );
  quarter := EXTRACT(QUARTER from dt);
  startYear := dateYear;
  endYear := dateYear+1;
  if ( quarter = 1 ) then
     startYear := startYear-1;
	 endYear := dateYear;
  end if;
  quarter := quarter-1;
  if ( quarter = 0 ) then
   	quarter := 4;
  end if;
  RETURN CONCAT(startYear, '-', endYear, '-Q', quarter);
end;
$BODY$;

CREATE OR REPLACE FUNCTION public.fiscal_year(
	dt timestamp without time zone)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
declare 
   dateYear INT;
   quarter INT;
   startYear INT;
   endYear INT;
begin
  dateYear := EXTRACT( YEAR from dt );
  quarter := EXTRACT(QUARTER from dt);
  startYear := dateYear;
  endYear := dateYear+1;
  if ( quarter = 1 ) then
     startYear := startYear-1;
	 endYear := dateYear;
  end if;

  RETURN CONCAT(startYear, '-', endYear);
end;
$BODY$;

