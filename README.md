# Library Management API

This is a Library Management API built using ASP.NET Core, providing endpoints to manage books, book copies, users, and loan records in a library system.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server (local or remote)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/klochan4/LibraryManagementAPI.git
   cd library-management-api
   
   

# Library Management API Documentation

## Overview

The Library Management API allows for managing books, book copies, users, and loan records in a library system. This API provides endpoints to create, read, update, and delete resources.

## Endpoints

### Books

- `GET /api/v1/books`: Retrieve all books.
- `GET /api/v1/books/{id}`: Retrieve a book by ID.
- `POST /api/v1/books`: Add a new book.
- `PUT /api/v1/books/{id}`: Update an existing book by ID.
- `DELETE /api/v1/books/{id}`: Delete a book by ID.

### Book Copies

- `GET /api/v1/bookcopies`: Retrieve all book copies.
- `GET /api/v1/bookcopies/{id}`: Retrieve a book copy by ID.
- `POST /api/v1/bookcopies`: Add a new book copy.
- `PUT /api/v1/bookcopies/{id}`: Update an existing book copy by ID.
- `DELETE /api/v1/bookcopies/{id}`: Delete a book copy by ID.

### Users

- `GET /api/v1/users`: Retrieve all users.
- `GET /api/v1/users/{id}`: Retrieve a user by ID.
- `POST /api/v1/users`: Add a new user.
- `PUT /api/v1/users/{id}`: Update an existing user by ID.
- `DELETE /api/v1/users/{id}`: Delete a user by ID.

### Loans

- `GET /api/v1/loans`: Retrieve all loan records.
- `GET /api/v1/loans/{id}`: Retrieve a loan record by ID.
- `POST /api/v1/loans`: Create a new loan record.
- `PUT /api/v1/loans/{id}/return`: Mark a loan as returned.
- `DELETE /api/v1/loans/{id}`: Delete a loan record by ID.

## Models

### Book

- `BookId` (int): The unique identifier for a book.
- `Title` (string): The title of the book.
- `Author` (string): The author of the book.
- `GenreProp` (enum): The genre of the book (e.g., Mystery, Romance, SciFi).
- `Description` (string): A description of the book.

### BookCopy

- `CopyId` (int): The unique identifier for a book copy.
- `BookId` (int): The ID of the associated book.
- `IsAvailable` (bool): The availability status of the book copy.

### User

- `UserId` (int): The unique identifier for a user.
- `Name` (string): The name of the user.
- `Email` (string): The email address of the user.

### LoanRecord

- `LoanRecordId` (int): The unique identifier for a loan record.
- `CopyId` (int): The ID of the loaned book copy.
- `UserId` (int): The ID of the user who borrowed the book.
- `LoanDate` (DateTime): The date the book was loaned.
- `ExpectedReturnDate` (DateTime): The expected return date of the book.
- `ActualReturnDate` (DateTime?): The actual return date of the book (nullable).

## Validation

- **Book**: Title is required and must be between 1 and 100 characters. Genre must be a valid value.
- **BookCopy**: CopyId and BookId are required. Status is required.
- **User**: Name is required and must be between 1 and 100 characters. Email is required and must be valid.
- **LoanRecord**: LoanDate and ExpectedReturnDate are required. ExpectedReturnDate must be after LoanDate.

## Logging

The API uses a logging mechanism to track actions and errors within the system. Logs provide useful information for debugging and monitoring the application.

## Error Handling

Errors are returned with appropriate HTTP status codes and messages to indicate the nature of the issue. Validation errors return `400 Bad Request`, not found resources return `404 Not Found`, and internal server errors return `500 Internal Server Error`.
