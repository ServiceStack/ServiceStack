---
title: Deployment Guide
---

# Introduction

In today's DevOps ecosystem, GitHub Actions stand out as an invaluable asset for automating CI/CD workflows directly within your GitHub repository. The introduction of .NET 8 takes this a step further, offering a streamlined approach to generating Docker images through the `<PublishProfile>DefaultContainer</PublishProfile>` setting in your `.csproj` files. This ensures consistent application packaging, making it deployment-ready by just using `dotnet publish`.

The new ServiceStack templates in .NET 8 bring additional flexibility. They are designed with cloud-agnosticism in mind, utilizing foundational tools like Docker for containerization and SSH for secure deployment. This makes it possible to deploy your applications to any Linux server, irrespective of the cloud provider you're using.

This guide aims to walk you through the hosting setup and the GitHub Actions release process as introduced in the new .NET 8 and ServiceStack templates.



# The Anatomy of the GitHub Actions Workflow

GitHub Actions workflows are defined in YAML files, and they provide a powerful way to automate your development process. This guide will take you through the key sections of the workflow to give you a comprehensive understanding of how it functions.

## Permissions

In this workflow, two permissions are specified:

- `packages: write`: This allows the workflow to upload Docker images to GitHub Packages.
- `contents: write`: This is required to access the repository content.

Specifying permissions ensures that the GitHub Actions runner has just enough access to perform the tasks in the workflow.

## Jobs

This workflow consists of two jobs: `push_to_registry` and `deploy_via_ssh`.

### push_to_registry

This job runs on an Ubuntu 22.04 runner and is responsible for pushing the Docker image to the GitHub Container Registry. It proceeds only if the previous workflow did not fail. The job includes the following steps:

1. **Checkout**: Retrieves the latest or specific tag of the repository's code.
2. **Env variable assignment**: Assigns necessary environment variables for subsequent steps.
3. **Login to GitHub Container Registry**: Authenticates to the GitHub Container Registry.
4. **Setup .NET Core**: Prepares the environment for .NET 8.
5. **Build and push Docker image**: Creates and uploads the Docker image to GitHub Container Registry (ghcr.io).

### deploy_via_ssh

This job also runs on an Ubuntu 22.04 runner and depends on the successful completion of the `push_to_registry` job. Its role is to deploy the application via SSH. The steps involved are:

1. **Checkout**: Retrieves the latest or specific tag of the repository's code.
2. **Repository name fix and env**: Sets up necessary environment variables.
3. **Create .env file**: Generates a .env file required for deployment.
4. **Copy files to target server via scp**: Securely copies files to the remote server.
5. **Run remote db migrations**: Executes database migrations on the remote server.
6. **Remote docker-compose up via ssh**: Deploys the Docker image with the application.

## Triggers (on)

The workflow is designed to be triggered by:

1. **New GitHub Release**: Activates when a new release is published.
2. **Successful Build action**: Runs whenever the specified Build action completes successfully on the main or master branches.
3. **Manual trigger**: Allows for rollback to a specific release or redeployment of the latest release, with an input for specifying the version tag.

Understanding these sections will help you navigate and modify the workflow as per your needs, ensuring a smooth and automated deployment process.

## Deployment Server Setup Expanded

### Ubuntu as the Reference Point

Though our example leverages Ubuntu, it's important to emphasize that the primary requirements for this deployment architecture are a Linux operating system, Docker, and SSH. Many popular Linux distributions like CentOS, Fedora, or Debian will work just as efficiently, provided they support Docker and SSH.

### The Crucial Role of SSH in GitHub Actions

**SSH** (Secure SHell) is not just a protocol to remotely access your server's terminal. In the context of GitHub Actions:

- SSH offers a **secure communication channel** between GitHub Actions and your Linux server.
- Enables GitHub to **execute commands directly** on your server.
- Provides a mechanism to **transfer files** (like Docker-compose configurations or environment files) from the GitHub repository to the server.

By generating a dedicated SSH key pair specifically for GitHub Actions (as detailed in the previous documentation), we ensure a secure and isolated access mechanism. Only the entities possessing the private key (in this case, only GitHub Actions) can initiate an authenticated connection.

### Docker & Docker-Compose: Powering the Architecture

**Docker** encapsulates your ServiceStack application into containers, ensuring consistency across different environments. Some of its advantages include:

- **Isolation**: Your application runs in a consistent environment, irrespective of where Docker runs.
- **Scalability**: Easily replicate containers to handle more requests.
- **Version Control for Environments**: Create, maintain, and switch between different container images.

**Docker-Compose** extends Docker's benefits by orchestrating the deployment of multi-container applications:

- **Ease of Configuration**: Describe your application's entire stack, including the application, database, cache, etc., in a single YAML file.
- **Consistency Across Multiple Containers**: Ensures that containers are spun up in the right order and with the correct configurations.
- **Simplifies Commands**: Instead of a long string of Docker CLI commands, a single `docker-compose up` brings your whole stack online.

### NGINX Reverse Proxy: The Silent Workhorse

Using an **nginx reverse proxy** in this deployment design offers several powerful advantages:

- **Load Balancing**: Distributes incoming requests across multiple ServiceStack applications, ensuring optimal resource utilization.
- **TLS Management**: Together with its companion container, nginx reverse proxy automates the process of obtaining and renewing TLS certificates. This ensures your applications are always securely accessible over HTTPS.
- **Routing**: Directs incoming traffic to the correct application based on the domain or subdomain.
- **Performance**: Caches content to reduce load times and reduce the load on your ServiceStack applications.

With an nginx reverse proxy, you can host multiple ServiceStack (or non-ServiceStack) applications on a single server while providing each with its domain or subdomain.



## Step-by-Step Implementation

### 1. Create Your ServiceStack Application

Start by creating your ServiceStack application. You can utilize the .NET `x` tool from ServiceStack or directly use a template from the `NetCoreTemplates` GitHub Organization. Here's how you can do it with the `x` tool:

```bash
dotnet tool install --global x
x new web YourApp
```

Replace `YourApp` with your desired project name. This will generate a new ServiceStack application with the necessary GitHub Action workflows already incorporated.

### 2. Configure DNS for Your Application

You need a domain to point to your Linux server. Create an A Record in your DNS settings that points to the IP address of your Linux server:

- **Subdomain**: e.g., `app.yourdomain.com`
- **Record Type**: A
- **Value/Address**: IP address of your Linux server

This ensures that any requests to `app.yourdomain.com` are directed to your server.

### 3. Setting Up GitHub Secrets

Navigate to your GitHub repository's settings, find the "Secrets and variables" section, and add the following secrets:

- **`DEPLOY_HOST`**: The IP address or hostname of your server.
- **`DEPLOY_USERNAME`**: The username for SSH login.
- **`DEPLOY_KEY`**: The private key you generated for GitHub Actions to SSH into your server.
- **`LETSENCRYPT_EMAIL`**: Your email address for Let's Encrypt notifications.

Use the GitHub CLI for a quicker setup, as illustrated in the previous guidelines.

### 4. Push to Main Branch to Trigger Deployment

With everything set up, pushing code to the main branch of your repository will trigger the GitHub Action workflow, initiating the deployment process:

```bash
git add .
git commit -m "Initial commit"
git push origin main
```

### 5. Verifying the Deployment

After the GitHub Actions workflow completes, you can verify the deployment by:

- Checking the workflow's logs in your GitHub repository to ensure it completed successfully.
- Navigating to your application's URL (e.g., `https://app.yourdomain.com`) in a web browser. You should see your ServiceStack application up and running with a secure HTTPS connection.



# Additional Resources

## ServiceStack

- **[Official Documentation](https://docs.servicestack.net/)**: Comprehensive guides and best practices for ServiceStack.
- **[ServiceStack Website](https://servicestack.net/)**: Explore features, templates, and client libraries.

## Docker & Docker-Compose

- **[Docker Documentation](https://docs.docker.com/)**: Core concepts, CLI usage, and practical applications.
- **[Docker-Compose Documentation](https://docs.docker.com/compose/)**: Define and manage multi-container applications.

## GitHub Actions

- **[GitHub Actions Documentation](https://docs.github.com/en/actions)**: Creating workflows, managing secrets, and automation tips.
- **[Starter Workflows](https://github.com/actions/starter-workflows)**: Templates for various languages and tools.

## SSH & Security

- **[SSH Key Management](https://www.ssh.com/academy/ssh/keygen)**: Guidelines on generating and managing SSH keys.
- **[GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)**: Securely store and use sensitive information.