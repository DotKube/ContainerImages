#!/bin/bash

# Debugging: Echo the MSSQL_SA_PASSWORD for verification
echo "MSSQL_SA_PASSWORD is: $MSSQL_SA_PASSWORD"

dbstatus=1
errcode=1
start_time=$SECONDS
end_by=$((start_time + 600))  # Wait for up to 120 seconds for SQL Server startup

echo "Starting check for SQL Server start-up at $start_time, will end at $end_by"

while [[ $SECONDS -lt $end_by && ( $errcode -ne 0 || ( -z "$dbstatus" || $dbstatus -ne 0 ) ) ]]; do
    echo "Checking for SQL Server start-up at $SECONDS"
    dbstatus="$(/opt/mssql-tools18/bin/sqlcmd -h -1 -t 1 -U sa -P "$MSSQL_SA_PASSWORD" -Q "SET NOCOUNT ON; Select SUM(state) from sys.databases" -S 127.0.0.1 -C)"
    errcode=$?
    sleep 1
done

# Fixed wait time calculation and output message
elapsed_time=$((SECONDS - start_time))

# Fixed the echo statement to ensure proper formatting
echo "Stopped checking for SQL Server start-up after $elapsed_time seconds (dbstatus=$dbstatus, errcode=$errcode, seconds=$SECONDS)"

if [[ $dbstatus -ne 0 ]] || [[ $errcode -ne 0 ]]; then
    echo "SQL Server took more than 120 seconds to start up or one or more databases are not in an ONLINE state"
    echo "dbstatus = $dbstatus"
    echo "errcode = $errcode"
    exit 1
fi

# Loop through the .sql files in the /docker-entrypoint-initdb.d and execute them with sqlcmd
for f in /docker-entrypoint-initdb.d/*.sql
do
    echo "Processing $f file..."
    /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U sa -P "$MSSQL_SA_PASSWORD" -d master -i "$f" -C

done