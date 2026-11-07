

\# NWSDB Water Billing System



A web-based water billing management system developed as an academic software engineering project. The system provides customer billing functionality and integrates a partner application for payment-related operations.



\## Project Overview



The NWSDB Water Billing System is designed to support water billing operations through a structured application with an ASP.NET Core Web API, a customer-facing web interface, and a separate QuickPay partner application.



\## Main Components



\- \*\*NWSDB.API\*\* – ASP.NET Core Web API for backend functionality.

\- \*\*NWSDB.Client\*\* – Customer-facing web application built with HTML, CSS, and JavaScript.

\- \*\*QuickPay.PartnerApp\*\* – Separate ASP.NET Core application for partner payment operations.



\## Technologies Used



\- C#

\- ASP.NET Core Web API

\- HTML

\- CSS

\- JavaScript

\- Microsoft SQL Server

\- Entity Framework Core

\- Git and GitHub



\## Project Structure



```text

NWSDB/

├── NWSDB.API/

├── NWSDB.Client/

├── QuickPay.PartnerApp/

├── NWSDB.sln

└── README.md

```



\## Getting Started



\### Prerequisites



\- Visual Studio

\- .NET SDK compatible with the solution

\- Microsoft SQL Server

\- SQL Server Management Studio (SSMS)



\### Setup Instructions



1\. Clone this repository:



&#x20;  ```bash

&#x20;  git clone https://github.com/Irushika-se/NWSDB-Water-Billing-System.git

&#x20;  ```



2\. Open `NWSDB.sln` using Visual Studio.



3\. Configure the SQL Server connection string in your local application configuration.



4\. Configure the JWT signing key securely. Do not commit real passwords, connection strings, or secret keys to GitHub.



5\. Restore the NuGet packages and build the solution.



6\. Apply the Entity Framework Core migrations to your database.



7\. Configure and run the required application projects.



\## Security Note



Application secrets and local database settings should be configured on the developer's machine. Do not publish private credentials or production secret keys.



\## Project Purpose



This project was developed to apply practical knowledge of web application development, backend API development, database management, and software engineering concepts.



\## Author



\*\*Irushika-se\*\*



GitHub: \[Irushika-se](https://github.com/Irushika-se)



