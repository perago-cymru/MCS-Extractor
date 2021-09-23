/*============================

Expect the following parameters:

$table - the table being created
$id - the id field
$start_date - the start date for the table
$end_date - the end date for the table
$identifier - the customer identifier field
*/

CREATE OR REPLACE FUNCTION public.{$table}_related_records(win integer)
    RETURNS TABLE(recordid integer, setid integer, addrid character varying, submission date, closedate date) 
    LANGUAGE 'plpgsql'
AS $BODY$
declare
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
   open neighbourCursor for SELECT req.{$identifier} as addrId,
                req.{$start_date},
                                req.{$end_date},
                                req.{$id} AS "id",
                                neighbours.{$id} AS neighbour_id
                                FROM {$table} req
                                LEFT JOIN {$table} neighbours
                                ON neighbours.{$identifier} = req.{$identifier}
                                WHERE
									 (neighbours.{$start_date}, neighbours.{$end_date})
									 OVERLAPS ( (req.{$start_date} -  (win || ' days')::interval ), (req.{$end_date} + (win || ' days')::interval)) 
                                AND req.id != neighbours.id
                                AND req.{$identifier} is not null
                                ORDER BY addrid;
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


CREATE VIEW {$table}_delivery_periods AS SELECT EXTRACT(YEAR from req.{$start_date}) ||'-'|| EXTRACT(MONTH from req.{$start_date}) as "month",
	   COUNT(req.{$id}) as submissions,
	   AVG(req.{$end_date} - req.{$start_date}),
	   COUNT(req.{$id}) FILTER(WHERE (req.{$end_date} - req.{$start_date} <14 )) AS "less than a fortnight",
	   COUNT(req.{$id}) FILTER(WHERE (req.{$end_date} - req.{$start_date} > 14 ) AND (req.{$end_date} - req.{$start_date} < 28 )) AS "two to four weeks",
	   COUNT(req.{$id}) FILTER(WHERE (req.{$end_date} - req.{$start_date} > 28  )) AS "over four weeks",
	   AVG(del.deliveries) as "closed daily",
	   percentile_cont(0.5) within group(  order by del.deliveries ) as "median closed daily"
FROM {$table} req
LEFT JOIN 
    ( SELECT * FROM  (SELECT req.{$end_date} as {$end_date}, count({$id}) as deliveries from {$table} req WHERE EXTRACT (isodow FROM req.{$end_date}) < 6 AND req.status='Delivered' GROUP BY  req.{$end_date}) ned 
	 WHERE ned.deliveries < 50 ) del
     ON del.{$end_date} = req.{$end_date}
WHERE req.status = 'Delivered'
AND 1 < (req.{$end_date} - req.{$start_date})
GROUP BY EXTRACT( MONTH from req.{$start_date}), EXTRACT(YEAR from req.{$start_date})
ORDER BY EXTRACT(YEAR from req.{$start_date}), EXTRACT(MONTH from req.{$start_date});

CREATE VIEW {$table}_quarterly_durations AS SELECT 
	   fiscal_quarter(req.{$start_date}) as "quarter",
	   COUNT(distinct req.{$id}) as "request count",
	   COUNT(distinct rel.setId) AS "user count",
      (COUNT(distinct req.{$id}) - COUNT(distinct rel.setId)) AS "duplicate count",
	   AVG(req.{$end_date} - req.{$start_date}) "mean duration",
	   percentile_cont(0.5) within group ( order by req.{$end_date} - req.{$start_date} ) as "median duration"
	   FROM {$table} req 
	   INNER JOIN {$table}_related_records(0) rel
	   ON req.{$id} = rel.recordId
	   WHERE 0 < (req.{$end_date}-req.{$start_date})
	   GROUP BY fiscal_quarter(req.{$start_date});


CREATE VIEW {$table}_duplicates AS
 WITH related (recordid, setid, addrid, submission, closedate) as ( SELECT * FROM {$table}_related_records(0) )
 SELECT rel.setid AS "request_set",
    req.{$id},
	setSizes.setSize - count({$id}) as "duplicates"
   FROM {$table} req
     INNER JOIN related rel 
	 ON req.{$id} = rel.recordid
	 INNER JOIN (SELECT setId as currentSet, count(recordid) as setSize FROM related GROUP BY setId) setSizes
	 ON rel.setId = setSizes.currentSet 
  GROUP BY req.{$id}, rel.setId, setSizes.setSize
  ORDER BY rel.setid;

