/*============================

Expect the following parameters:

$table - the table being created
$id - the id field
$start_date - the start date for the table
$end_date - the end date for the table
$identifier - the customer identifier field
*/

-- NOT YET IN USE!


CREATE VIEW sourced SELECT min(receptacle_requests_sourced.id) AS id,
    avg(receptacle_requests_sourced.close_date - receptacle_requests_sourced.submission) AS duration,
    st_centroid(st_union(receptacle_requests_sourced.point_location)) AS centre,
    receptacle_requests_sourced.postcode
   FROM receptacle_requests_sourced
  GROUP BY receptacle_requests_sourced.postcode;

CREATE VIEW q1_requests AS  SELECT receptacle_requests_sourced.id,
    receptacle_requests_sourced.service_request,
    receptacle_requests_sourced.status,
    receptacle_requests_sourced.request_name,
    receptacle_requests_sourced.mobile,
    receptacle_requests_sourced.submission,
    receptacle_requests_sourced.reported_by,
    receptacle_requests_sourced.close_date,
    receptacle_requests_sourced.address,
    receptacle_requests_sourced.uprn,
    receptacle_requests_sourced.usrn,
    receptacle_requests_sourced.lpi,
    receptacle_requests_sourced.longitude,
    receptacle_requests_sourced.latitude,
    receptacle_requests_sourced.full_address,
    receptacle_requests_sourced.owner,
    receptacle_requests_sourced.question1,
    receptacle_requests_sourced.first_name,
    receptacle_requests_sourced.question2,
    receptacle_requests_sourced.last_name,
    receptacle_requests_sourced.question3,
    receptacle_requests_sourced.contact_no,
    receptacle_requests_sourced.question4,
    receptacle_requests_sourced.email,
    receptacle_requests_sourced.question_5,
    receptacle_requests_sourced.swap,
    receptacle_requests_sourced.question_6,
    receptacle_requests_sourced.requested_service,
    receptacle_requests_sourced.question_7,
    receptacle_requests_sourced.requested_items,
    receptacle_requests_sourced.question_8,
    receptacle_requests_sourced.requested_swap,
    receptacle_requests_sourced.question_9,
    receptacle_requests_sourced.additional_comments,
    receptacle_requests_sourced.question_10,
    receptacle_requests_sourced.user_research_response,
    receptacle_requests_sourced.address_line_1,
    receptacle_requests_sourced.postcode,
    receptacle_requests_sourced.point_location
   FROM receptacle_requests_sourced
  WHERE date_part('quarter'::text, receptacle_requests_sourced.submission) = 1::double precision;
