# TFG_Back

This repository contains the backend service for a university management application, developed as a Final Degree Project (TFG). It is built with .NET 8 and ASP.NET Core, utilizing Supabase for database, authentication, and file storage. The system is designed to manage students, teachers, subjects, groups, and academic schedules, with a focus on automating the process of group assignments and handling student change requests.

## Core Features

*   **User & Authentication Management**: Secure user registration, login (email/password and OAuth), and session management using Supabase Auth. Differentiated roles for `admin`, `teacher`, and `student`.
*   **Academic Structure**: CRUD operations for managing subjects, student groups, physical locations (classrooms), and their capacities.
*   **Scheduling System**: Create, update, and delete class schedules, with robust conflict detection to prevent overlaps for locations, groups, teachers, and subjects within the same course.
*   **Group Change Requests**: A complete workflow for students to request a change of group. Students can submit requests with justifications (including PDF uploads), which can then be managed by teachers or an automated system.
*   **Automated Group Management**:
    *   **Round-Robin Distribution**: An algorithm to automatically and evenly distribute students without a group into available subject groups based on capacity.
    *   **Min-Cost Max-Flow Optimization**: An advanced algorithm to intelligently process pending group change requests. It optimizes for group balance and available capacity, automatically accepting requests that improve the overall distribution or form direct swaps between students.
*   **Scheduled Tasks**: API endpoints secured with an API key, designed to be called by cron jobs to periodically run the automated group management algorithms.

## Technology Stack

*   **Backend**: .NET 8, ASP.NET Core Web API
*   **Database & Auth**: [Supabase](https://supabase.com/) (PostgreSQL, GoTrue for authentication, Storage)
*   **Containerization**: Docker

## Algorithms

The system leverages several algorithms to automate and optimize administrative tasks:

### 1. Round-Robin Student Distribution
This algorithm assigns students who are enrolled in a subject but not yet in a group. It iterates through the available groups for that subject, assigning one student at a time to each group in a cyclical manner, respecting the total capacity of each group's scheduled locations. This ensures a fair and balanced initial distribution of students. This process can be triggered via the `api/algorithms/FillGroups` endpoint.

### 2. Min-Cost Max-Flow Request Resolution
For handling group change requests, the system models the problem as a min-cost max-flow network:
*   **Nodes**: The graph includes a source, a sink, nodes for each pending request, and nodes for each group.
*   **Edges & Costs**:
    *   The source is connected to each request.
    *   Each request is connected to its desired destination group. The *cost* of this edge is weighted by the student's justification (`Weight`) and penalized by the destination group's current occupancy to favor less crowded groups.
    *   Groups are connected to the sink with an edge *capacity* equal to their remaining free spots.
*   **Outcome**: By finding the minimum cost flow, the algorithm identifies the most "beneficial" requests to accept without exceeding group capacities. It also detects and accepts direct swaps between two students automatically. This process is triggered via the `api/algorithms/CheckRequests` endpoint.

## API Structure

The application exposes a RESTful API organized by resource:

*   **/api/auth**: Handles user registration, login, logout, and session refreshing.
*   **/api/profiles**: Manages user profile data, including fetching all users (admin only) and updating user roles or subjects.
*   **/api/subjects**: CRUD operations for academic subjects.
*   **/api/groups**: Manages student groups, including student lists and assignments.
*   **/api/locations**: CRUD operations for classrooms and other physical locations.
*   **/api/schedules**: Manages the scheduling of group sessions, including conflict validation.
*   **/api/requests**: Manages the lifecycle of student group change requests.
*   **/api/algorithms**: Provides endpoints for cron jobs to trigger the automated group distribution and request resolution processes.
*   **/api/control**: Allows toggling system-wide features, such as enabling or disabling the group request functionality.

## Local Setup

### Prerequisites

*   .NET 8 SDK
*   A Supabase project

### Configuration

1.  Clone the repository:
    ```bash
    git clone https://github.com/PB-421/TFG_Back.git
    cd TFG_Back
    ```

2.  Create a `.env` file in the root of the project with the following environment variables obtained from your Supabase project dashboard:
    ```env
    # Supabase Project URL
    DB_URL="https://<your-project-ref>.supabase.co"

    # Supabase Service Role Key (for admin operations)
    DB_SUDOKEY="<your-service-role-key>"
    
    # Supabase Anon Public Key
    DB_KEY="<your-anon-public-key>"

    # Supabase JWT Secret (from JWT Settings)
    SUPABASE_JWT_SECRET="<your-jwt-secret>"

    # A custom secret key for securing cron job endpoints
    CRON_SECRET_KEY="<your-secure-random-string>"
    ```

### Running the Application

Execute the following command in the project root:

```bash
dotnet run
```

The API will be available at `http://localhost:5133`, with the Swagger UI at `http://localhost:5133/swagger`.

## Docker Deployment

The repository includes a `Dockerfile` for containerizing the application. It is configured to work with hosting platforms like Render that use a dynamic `PORT` environment variable.

1.  **Build the Docker image:**
    ```bash
    docker build -t tfg_back .
    ```

2.  **Run the Docker container:**
    Pass the required environment variables from your `.env` file to the container.
    ```bash
    docker run -d -p 8080:8080 \
      -e PORT=8080 \
      -e DB_URL="https://<your-project-ref>.supabase.co" \
      -e DB_SUDOKEY="<your-service-role-key>" \
      -e DB_KEY="<your-anon-public-key>" \
      -e SUPABASE_JWT_SECRET="<your-jwt-secret>" \
      -e CRON_SECRET_KEY="<your-secure-random-string>" \
      --name tfg-back-container tfg_back
    ```

## ⚖️ License Information

This project is licensed under the **MIT License**.

You are free to use, modify, and distribute this software, provided that the
original copyright notice and this permission notice are included.

For more details, see the `LICENSE` file in this repository.

The application will be accessible at `http://localhost:8080`.
