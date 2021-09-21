
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

