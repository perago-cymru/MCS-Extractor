
CREATE DATABASE "mcs_extracted_data"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'C'
    LC_CTYPE = 'C'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

USE mcs_extracted_data;

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

CREATE OR REPLACE FUNCTION public.find_related_records(win integer, tableName varchar, idField varchar, startField varchar, closeField varchar, uniqueField varchar)
    RETURNS TABLE(recordid integer, setid integer, addrid character varying, submission date, closedate date) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
declare
   queryText varchar;
   neighbourCursor refcursor;
   addId varchar; 
   open_date date; 
   close_date date; 
   row_id int; 
   neighbour_id int;
   setId int;
   current_row_id int;
   current_set_id int;
   neighbour_row_id int;
   neighbour_set_id int;
   startGap int;
   endGap int;
begin 
   CREATE TEMP TABLE outcome(recordId int, setId int, addrId varchar, submission Date, closeDate Date) on commit drop;
   setId := 1;
   queryText := FORMAT( 'SELECT req.'||uniqueField||' as addrId,
    	req.submission,
		req.'||closeField||',
		req.'||idField||' AS "id",
		neighbours.'||idField||' AS neighbour_id
		FROM '||tableName||' req 
		LEFT JOIN '||tableName||' neighbours 
		ON neighbours.'||uniqueField||' = req.'||uniqueField||' 
		WHERE
			(neighbours.'||startField||', neighbours.'||closeField||') 
			OVERLAPS ( (req.'||startField||' -  ( $1 || '' days'')::interval ), (req.'||closeField||' + ( $1 || '' days'')::interval))  
		AND req.'||idField||' != neighbours.'||idField||'
		AND req.'||uniqueField||' is not null
		ORDER BY req.'||uniqueField);
	raise notice 'Query: % ', queryText;
	OPEN neighbourCursor FOR EXECUTE queryText USING win;	
		loop
           fetch next from neighbourCursor into addId, open_date, close_date, row_id, neighbour_id;
		   exit when not found;
		   SELECT * FROM outcome INTO current_row_id, current_set_id WHERE outcome.recordId = row_id AND outcome.setId IS NOT NULL;
		   SELECT * FROM outcome INTO neighbour_row_id, neighbour_set_id 
		          WHERE outcome.recordId = neighbour_id 
				  AND outcome.setId IS NOT NULL;
		   if ( current_row_id is null ) then
		   	  if (neighbour_row_id is null) then 
			      if (neighbour_id is not null ) then 
			      	INSERT INTO outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( neighbour_id, setId, addId, open_date, close_date );
				  end if;
			  	  INSERT INTO outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( row_id, setId, addId, open_date, close_date );
				  setId := setId+1;
			  else
			      INSERT INTO outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( row_id, neighbour_set_id, addId, open_date, close_date );
			  end if;
		   else 
		      if ( neighbour_row_id is null ) then 
			  	 INSERT INTO outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( neighbour_id, current_set_id, addId, open_date, close_date);
			  else
			     if (neighbour_set_id != current_set_id AND current_set_id IS NOT NULL ) then
				     UPDATE outcome SET setId = current_set_id WHERE outcome.setId = neighbour_set_id;
				 end if;
			  end if;
		   end if;
		end loop;
		CLOSE neighbourCursor;
		RETURN QUERY SELECT * FROM outcome;
end;
$BODY$;


