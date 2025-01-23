# Container Images

A repository of curated, pre-configured container images and Compose templates tailored for developers and DevOps engineers. These images are built with secure, optimized configurations and support advanced use cases, such as SQL Server with Full-Text Search, self-hosted pipeline agents, and AlmaLinux-based images.

## 📝 Features

1. **Pre-configured Images**
   - **AlmaLinux 9.5 Self-Hosted Pipeline Agent**  
     A lightweight and secure containerized pipeline agent, ideal for self-hosted Azure DevOps or similar setups.
   - **SQL Server with Full-Text Search (Ubuntu)**  
     A preconfigured SQL Server image with Full-Text Search (FTS) capabilities enabled.
   - **AlmaLinux MSSQL Tools**  
     A compact AlmaLinux-based image for working with SQL Server tools like `sqlcmd` and `bcp`.

2. **Automation with Taskfiles**
   - Each directory includes a `Taskfile.yaml` to simplify building, testing, and managing containers.

3. **Compose Templates**
   - Ready-to-use `compose.yaml` files make it easy to integrate these images into local development or CI/CD workflows.

4. **Integration Testing**
   - Includes test suites for validating container functionality, ensuring reliability in production.

## 🤝 Contributing

Contributions are welcome! If you have ideas for additional images or improvements, feel free to open an issue or submit a pull request.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
