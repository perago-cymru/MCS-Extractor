/*============================

Expect the following parameters:

$table - the table being created
$id - the id field
$start_date - the start date for the table
$end_date - the end date for the table
$identifier - the customer identifier field
*/

-- NOT YET IN USE!

CREATE VIEW {$table}_overlapping_calls AS SELECT * FROM find_related_records(1, '{$table}', '{$id}', '{$start_date}', '{$end_date}', '{$identifier}');


