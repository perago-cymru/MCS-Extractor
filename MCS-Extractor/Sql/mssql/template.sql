/*
   {$table} replaces {$table}
   {$start_date} replaces {$start_date}
   {$end_date} replaces {$end_date}
   {$id} replaces {$id}
   {$identifier} replaces {$identifier}
*/
CREATE OR ALTER FUNCTION dbo.{$table}_related_records(@win integer)
	RETURNS @outcome TABLE(recordid integer, setid integer, addrid character varying, submission date, closedate date) 
AS 
BEGIN
declare
	@addId varchar,
	@open_date date,
	@close_date date,
	@row_id int,
	@neighbour_id int,
	@setId int,
	@current_row_id int,
	@current_set_id int,
	@neighbour_row_id int,
	@neighbour_set_id int,
	@startGap int,
	@endGap int;
	SET @setId = 1;
	DECLARE neighbourCursor CURSOR FOR SELECT req.{$identifier} as addrId,
								req.{$start_date},
								req.{$end_date},
								req.{$id} AS "id",
								neighbours.{$id} AS neighbour_id
								FROM {$table} req
								LEFT JOIN {$table} neighbours
								ON neighbours.{$identifier} = req.{$identifier}
								WHERE (SELECT CASE WHEN 
											(( DATEADD(day, (@win * -1), req.{$start_date}) <= neighbours.{$end_date})
												AND ( DATEADD(day, @win , req.{$end_date}) > neighbours.{$end_date}))
											OR 
											(( DATEADD(day, (@win * -1), req.{$start_date}) <= neighbours.{$start_date})
												AND ( DATEADD(day, @win , req.{$end_date}) > neighbours.{$start_date}))
											OR (( DATEADD(day, (@win * -1), req.{$start_date}) <= neighbours.{$start_date})
												AND ( DATEADD(day, @win , req.{$end_date}) >= neighbours.{$end_date}))
											THEN 1 ELSE 0 END) =  1
								AND req.id != neighbours.id
								AND req.{$identifier} is not null
								ORDER BY addrid;
	OPEN neighbourCursor;
		fetch next from neighbourCursor into @addId, @open_date, @close_date, @row_id, @neighbour_id;
		WHILE @@FETCH_STATUS = 0
			BEGIN
				SELECT  @current_row_id = recordId, @current_set_id= setId FROM @outcome
							WHERE recordId = @row_id AND setId IS NOT NULL;
								   
				SELECT @neighbour_row_id = recordId, @neighbour_set_id = setId FROM @outcome 
							WHERE recordId = @neighbour_id AND setId IS NOT NULL;

				if ( @current_row_id is null ) 
					if ( @neighbour_row_id is null) 
						BEGIN
							if ( @neighbour_id is not null ) 
								INSERT INTO @outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( @neighbour_id, @setId, @addId, @open_date, @close_date);
							INSERT INTO @outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( @row_id, @setId, @addId, @open_date, @close_date);
							set @setId = @setId+1;		   
						END
					else

						INSERT INTO @outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( @row_id, @neighbour_set_id, @addId, @open_date, @close_date );
				
				else
					if ( @neighbour_row_id is null ) 
			
						INSERT INTO @outcome ( recordId, setId, addrId, submission, closeDate) VALUES ( @neighbour_id, @setId, @addId, @open_date, @close_date );
					
					else

						if (@neighbour_set_id != @current_set_id AND @current_set_id IS NOT NULL ) 
							UPDATE @outcome SET setId = @current_set_id  WHERE setId = @neighbour_set_id;
					
			fetch next from neighbourCursor into @addId, @open_date, @close_date, @row_id, @neighbour_id;
			set @current_row_id = null;
			set @neighbour_row_id = null;
		END;
	CLOSE neighbourCursor;
	RETURN;
END;

GO

/*
* Replace
   {$table} replaces {$table}
   {$start_date} replaces {$start_date}
   {$end_date} replaces {$end_date}
   {$id} replaces {$id}
   {$identifier} replaces {$identifier}
*/

CREATE OR ALTER VIEW {$table}_delivery_periods AS SELECT CONCAT( YEAR(req.{$start_date}), '-', RIGHT('00' + CAST(DATEPART(mm, req.{$start_date}) AS VARCHAR), 2)) as "month",
	   COUNT(req.{$id}) as submissions,
	   AVG(DATEDIFF(day,  req.{$start_date}, req.{$end_date})) AS "mean turnaround",
	   SUM(CASE WHEN (req.{$end_date} - req.{$start_date} <14 ) THEN 1 ELSE 0 END) AS "less than a fortnight",
	   SUM(CASE WHEN ((req.{$end_date} - req.{$start_date} > 14 ) AND (req.{$end_date} - req.{$start_date} < 28 )) THEN 1 ELSE 0 END) AS "two to four weeks",
	   SUM(CASE WHEN ((req.{$end_date} - req.{$start_date} > 28  )) THEN 1 ELSE 0 END) AS "over four weeks",
		del.mean as "mean close time",
		del.median as "median close time"
		FROM {$table} req
INNER JOIN 
    ( SELECT 
    DISTINCT
	 DATEPART(year, ned.closed_on) as "year",
	 DATEPART(month, ned.closed_on) as "month",
	  SUM(ned.deliveries) OVER (PARTITION BY ned.yearmon) AS "count",
	  AVG(ned.deliveries) OVER (PARTITION BY ned.yearmon) AS "mean",
	 PERCENTILE_DISC(0.5) WITHIN GROUP (ORDER BY ned.deliveries) OVER (PARTITION BY ned.yearmon) as "median"
  FROM  ( SELECT  CONVERT(date, req.{$end_date}) as closed_on,
				  (YEAR(CONVERT(date, req.{$end_date}))*100) + MONTH(CONVERT(date, req.{$end_date})) as "yearmon",
                 COUNT(distinct({$id})) as deliveries 
	                         from {$table} req 
							 WHERE DATEPART (dw, req.{$end_date}) < 6 AND req.status='Delivered' 
							 GROUP BY  Convert(date, req.{$end_date})) ned WHERE ned.deliveries < 50 )  del
     ON (del.year = DATEPART(year, req.{$start_date}) AND del.month= DATEPART(month, req.{$start_date}))
WHERE req.status = 'Delivered'
AND 1 < DATEDIFF(day, req.{$start_date}, req.{$end_date})
GROUP BY DATEPART( month, req.{$start_date}), DATEPART(year, req.{$start_date}), del.median, del.mean;

GO

CREATE OR ALTER VIEW {$table}_quarterly_durations AS SELECT 
	   dbo.fiscal_quarter(req.{$start_date}) as "quarter",
	   COUNT(distinct req.{$id}) as "request count",
	   COUNT(distinct rel.setId) AS "user count",
      (COUNT(distinct req.{$id}) - COUNT(distinct rel.setId)) AS "duplicate count",
	   AVG( DATEDifF(dd, req.{$start_date}, req.{$end_date})) AS "mean duration" --,
	--   percentile_cont(0.5) within group ( order by DATEDifF(dd, req.{$start_date}, req.{$end_date})) OVER (PARTITION BY dbo.fiscal_quarter(req.{$start_date})) as "median duration"
	   FROM  {$table} req 
	   INNER JOIN dbo.{$table}_related_records(0) rel
	   ON req.{$id} = rel.recordId
	   WHERE 0 < (req.{$end_date}-req.{$start_date})
	   GROUP BY dbo.fiscal_quarter(req.{$start_date});

GO

CREATE OR ALTER VIEW {$table}_duplicates
AS
  WITH related (recordid, setid, addrid, submission, closedate) as ( SELECT * FROM {$table}_related_records(0) )
 SELECT rel.setid AS "request_set",
    req.{$id},
	setSizes.setSize as "duplicates"
   FROM {$table} req
     INNER JOIN related rel 
	 ON req.{$id} = rel.recordid
	 INNER JOIN (SELECT setId as currentSet, count(distinct recordid) as setSize FROM related GROUP BY setId) setSizes
	 ON rel.setId = setSizes.currentSet 
  GROUP BY req.{$id}, rel.setId, setSizes.setSize;

GO
