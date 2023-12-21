---
title: GitHub Action Deployments
---

In today's DevOps ecosystem, [GitHub Actions](https://github.com/features/actions) stand out as an invaluable asset for 
automating CI/CD workflows directly within your GitHub repository. The introduction of .NET 8 takes this a step further, 
offering a streamlined approach to generating Docker images through the `<PublishProfile>DefaultContainer</PublishProfile>` 
setting in your `.csproj`. This ensures consistent application packaging, making it deployment-ready by just using `dotnet publish`.

ServiceStack's project templates bring additional flexibility, by utilizing foundational tools like 
[Docker](https://www.docker.com) for containerization and [SSH](https://en.wikipedia.org/wiki/Secure_Shell) for secure deployments,
it's able to deploy your Dockerized .NET applications to any Linux server, whether self-hosted or on any cloud provider.

#### Live Demos use their GitHub Actions to deploy themselves

Each template's Live demo are themselves utilizing their included GitHub Actions to deploy itself to a Linux server running 
on a **€13.60 /month** shared 8BG RAM [Hetzner Cloud VM](https://www.hetzner.com/cloud) that's currently running 50+ Docker Containers.

This guide aims to walk you through the hosting setup and the GitHub Actions release process as introduced in the
ServiceStack's latest .NET 8 project templates.

## Deployment Files

Deployment files that are included in project templates to facilitate GitHub Actions deployments:

#### .deploy/
- [nginx-proxy-compose.yml](https://github.com/NetCoreTemplates/blazor/blob/master/.deploy/nginx-proxy-compose.yml) - Manage nginx reverse proxy and Lets Encrypt companion container (one-time setup per server)
- [docker-compose.yml](https://github.com/NetCoreTemplates/blazor/blob/main/.deploy/docker-compose.yml) - Manage .NET App Docker Container

#### .github/workflows/
- [build.yml](https://github.com/NetCoreTemplates/blazor/blob/master/.github/workflows/build.yml) - Build .NET Project and Run Tests
- [release.yml](https://github.com/NetCoreTemplates/blazor/blob/master/.github/workflows/release.yml) - Build container, Push to GitHub Packages Registry, SSH deploy to Linux server, Run DB Migrations and start new Docker Container if successful otherwise revert Migration

## Prerequisites

Before your Linux server can accept GitHub Actions deployments, we need to set up a few things:

### Setup Deployment Server

#### 1. Install Docker and Docker-Compose

Follow [Docker's installation instructions](https://docs.docker.com/engine/install/ubuntu/) 
to correctly install the latest version of Docker.

#### 2. Configure SSH for GitHub Actions

Generate a dedicated SSH key pair to be used by GitHub Actions:

:::sh
ssh-keygen -t rsa -b 4096 -f ~/.ssh/github_actions
:::

Add the **public key** to your server's SSH **authorized_keys**:

:::sh
cat ~/.ssh/github_actions.pub >> ~/.ssh/authorized_keys
:::

Then, add the **private key** to your repo's `DEPLOY_KEY` GitHub Action Secret which GitHub Actions will use to
securely SSH into the server.

#### 3. Set Up nginx-reverse-proxy

You should have a `docker-compose` file similar to the `nginx-proxy-compose.yml` in your repository. Upload this file to your server:

:::sh
scp nginx-proxy-compose.yml user@your_server:~/
:::

To bring up the nginx reverse proxy and its companion container for handling TLS certificates, run:

:::sh
docker compose -f ~/nginx-proxy-compose.yml up -d
:::

This will start an nginx reverse proxy along with a companion LetsEncrypt container that will automatically watch for 
additional Docker containers on the same network and initialize them with valid TLS certificates.

### Ready to host containerized .NET Apps

Your Linux server is now ready to accept multiple .NET App deployments from GitHub Actions. The guide below walks through 
the process of setting up your GitHub repository to deploy new ServiceStack .NET Apps to your Linux server.

## Step-by-Step Guide

### 1. Create Your ServiceStack Application

Start by creating your ServiceStack application, either from [ServiceStack's Start Page](https://servicestack.net/start) 
or by using the [x dotnet tool](https://docs.servicestack.net/dotnet-tool):

:::sh
x new blazor ProjectName
:::

Replace `ProjectName` with your desired project name to generate a new ServiceStack application pre-configured 
with the necessary Docker compose files and GitHub Action workflows as above.

### 2. Configure DNS for Your Application

You need a domain to point to your Linux server. Create an A Record in your DNS settings that points to the IP address 
of your Linux server:

- **Subdomain**: `app.example.org`
- **Record Type**: A
- **Value/Address**: IP address of your Linux server

This ensures that any requests to `app.example.org` are directed to your server.

### 3. Setting Up GitHub Secrets

Navigate to your GitHub repository's settings, find the "Secrets and variables" section, and add the following secrets:

- `DEPLOY_HOST`: IP address or hostname of your Linux server
- `DEPLOY_USERNAME`: SSH Username to use for deployments
- `DEPLOY_KEY`: Private key generated for GitHub Actions to SSH into your server
- `LETSENCRYPT_EMAIL`: Your email address for Let's Encrypt notifications

#### Using GitHub CLI for Secret Management

You can use the [GitHub CLI](https://cli.github.com/manual/gh_secret_set) for a quicker setup of these GitHub Action Secrets, e.g:

```bash
gh secret set DEPLOY_HOST --body="linux-server-host"
gh secret set DEPLOY_USERNAME --body="linux-server-username"
gh secret set DEPLOY_KEY --bodyFile="path/to/ssh-private-key"
gh secret set LETSENCRYPT_EMAIL --body="your-email@example.org"
```

These secrets will populate environment variables within your GitHub Actions workflow and other configuration files, 
enabling secure and automated deployment of your ServiceStack applications.

### 4. Push to Main Branch to Trigger Deployment

With everything set up, pushing code to the main branch of your repository will trigger the GitHub Action workflow, initiating the deployment process:

```bash
git add .
git commit -m "Initial commit"
git push origin main
```

### 5. Verifying the Deployment

After the GitHub Actions workflow completes, you can verify the deployment by:

- Checking the workflow's logs in your GitHub repository to ensure it completed successfully
- Navigating to your application's URL (e.g., `https://app.example.org`) in a web browser. You should see your ServiceStack application up and running with a secure HTTPS connection

## Features

### DB Migrations

The GitHub Actions workflow includes a step to run database migrations on the remote server in the **Run remote db migrations** step
which automatically runs the `migrate` AppTask in the `app-migration` companion Docker container on the Linux Host Server
to validate migration was successful before completing deployment of the new App. A failed migration will cause deployment to fail 
and the previous App version to continue to run.

### Patch appsettings.json with production secrets

One way to maintain sensitive information like API keys and connection strings for your Production App outside of its
source code GitHub repository is to patch the `appsettings.json` file with a [JSON Patch](https://jsonpatch.com) that's 
stored in your repo's `APPSETTINGS_PATCH` GitHub Action Secret which will be applied with the deployed App's `appsettings.json` file.

For example this JSON Patch below will replace values and objects in your App's **appsettings.json**: 

```json
[
    { "op":"add", "path":"/oauth.facebook.AppSecret",  "value":"xxxx" },
    { "op":"add", "path":"/oauth.microsoft.AppSecret", "value":"xxxx" },
    { "op":"add", "path":"/smtp", "value":{
        "UserName": "xxxx",
        "Password": "xxxx",
        "Host": "smtp-server.example.org",
        "Port": 587,
        "From": "noreply@example.org",
        "FromName": "No Reply"
      } 
    }
]
```

You can test your JSON Patch by saving it to `appsettings.json.patch` and applying it with the 
[patch feature](https://docs.servicestack.net/dotnet-tool#patch-json-files) of the `x` dotnet tool: 

:::sh
x patch appsettings.json.patch
:::

## Anatomy of GitHub Actions Workflow

GitHub Actions workflows are defined in YAML files, and they provide a powerful way to automate your development process. 
This guide will take you through the key sections of the workflow to give you a comprehensive understanding of how it functions.

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


## Additional Resources

### ServiceStack

- **[Official Documentation](https://docs.servicestack.net/)**: Comprehensive guides and best practices for ServiceStack.
- **[ServiceStack Website](https://servicestack.net/)**: Explore features, templates, and client libraries.

### Docker & Docker-Compose

- **[Docker Documentation](https://docs.docker.com/)**: Core concepts, CLI usage, and practical applications.
- **[Docker-Compose Documentation](https://docs.docker.com/compose/)**: Define and manage multi-container applications.

### GitHub Actions

- **[GitHub Actions Documentation](https://docs.github.com/en/actions)**: Creating workflows, managing secrets, and automation tips.
- **[Starter Workflows](https://github.com/actions/starter-workflows)**: Templates for various languages and tools.

### SSH & Security

- **[SSH Key Management](https://www.ssh.com/academy/ssh/keygen)**: Guidelines on generating and managing SSH keys.
- **[GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)**: Securely store and use sensitive information.