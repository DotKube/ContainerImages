using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Docker.DotNet;
using Docker.DotNet.Models;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace SqlServerTests
{
    public class SqlServerContainerTests : IAsyncLifetime
    {
        private DockerClient _dockerClient;
        private const string RegistryContainerName = "docker-registry";
        private const string RegistryImage = "registry:2";
        private const int RegistryPort = 5001;
        private const string ImageName = "localhost:5001/sql-server-full-text-search:latest";
        private const string ContainerName = "mssql-server-test";
        private const string SaPassword = "YourStrong!Password";
        private readonly string _connectionString = "Server=127.0.0.1,1433;Database=master;User Id=sa;Password=YourStrong!Password;TrustServerCertificate=true;Encrypt=false";

        public async Task InitializeAsync()
        {
            _dockerClient = CreateDockerClient();

            // Start the Docker registry
            // await StartRegistryAsync();

            // Build the image
            await BuildImageAsync(_dockerClient);

            // Remove any existing container with the same name
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { });
            foreach (var container in containers.Where(c => c.Names.Any(name => name.Contains(ContainerName))))
            {
                await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
            }

            // Create and start the SQL Server container
            await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Name = ContainerName,
                Image = ImageName,
                Env = new List<string>
                {
                    "ACCEPT_EULA=Y",
                    $"MSSQL_SA_PASSWORD={SaPassword}",
                    "MSSQL_AGENT_ENABLED=True"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { "1433/tcp", new List<PortBinding> { new PortBinding { HostPort = "1433" } } }
                    }
                }
            });

            await _dockerClient.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters());

            // Wait for SQL Server to start
            await Task.Delay(TimeSpan.FromSeconds(20));
        }

        public async Task DisposeAsync()
        {
            // Stop and remove the SQL Server container
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            foreach (var container in containers.Where(c => c.Names.Contains($"/{ContainerName}")))
            {
                await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters { WaitBeforeKillSeconds = 10 });
                await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
            }

            // Stop and remove the Docker registry container
            var registryContainers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            foreach (var container in registryContainers.Where(c => c.Names.Contains($"/{RegistryContainerName}")))
            {
                await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters { WaitBeforeKillSeconds = 10 });
                await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
            }

            _dockerClient.Dispose();
        }

        private async Task StartRegistryAsync()
        {
            Console.WriteLine("Starting Docker registry...");

            IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Limit = 10,
            })
            .ConfigureAwait(false);

            foreach (var container in containers)
            {
                Console.WriteLine(JsonSerializer.Serialize(container));
            }

            // Create and start the registry container
            Console.WriteLine("Creating and starting Docker registry container...");
            // await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            // {
            //     Name = RegistryContainerName,
            //     Image = RegistryImage,

            //     HostConfig = new HostConfig
            //     {
            //         PortBindings = new Dictionary<string, IList<PortBinding>>
            // {
            //     { "5000/tcp", new List<PortBinding> { new PortBinding { HostPort = "5001" } } }
            // }
            //     }
            // });

            await _dockerClient.Containers.StartContainerAsync(RegistryContainerName, new ContainerStartParameters());
            Console.WriteLine("Docker registry started on port 5001.");
        }



        private static async Task BuildImageAsync(DockerClient client)
        {

            Console.WriteLine("Building Docker image...");

            try
            {
                // Step 1: Define build context
                var sourcePath = AppDomain.CurrentDomain.BaseDirectory;
                var buildContextPath = Path.GetFullPath(Path.Combine(sourcePath, "..", "..", "..", "..", ".."));
                Console.WriteLine($"Resolved Build Context Path: {buildContextPath}");

                if (!Directory.Exists(buildContextPath))
                {
                    throw new DirectoryNotFoundException($"Build context directory not found: {buildContextPath}");
                }

                // Step 2: Create tarball archive
                var tarballPath = Path.Combine(Path.GetTempPath(), "docker-build-context.tar");
                CreateTarball(buildContextPath, tarballPath);
                Console.WriteLine($"Tarball created at: {tarballPath}");

                using (var tarStream = new MemoryStream(File.ReadAllBytes(tarballPath)))
                {
                    // Step 3: Extract tarball to locate Dockerfile
                    var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    Directory.CreateDirectory(tempDir);

                    using (var extractedArchive = SharpCompress.Archives.ArchiveFactory.Open(tarStream))
                    {
                        foreach (var entry in extractedArchive.Entries.Where(entry => !entry.IsDirectory))
                        {
                            entry.WriteToDirectory(tempDir, new SharpCompress.Common.ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }

                    // Step 4: Find the Dockerfile
                    var dockerfilePath = Directory.GetFiles(tempDir, "Dockerfile", SearchOption.AllDirectories).FirstOrDefault();
                    if (dockerfilePath == null)
                    {
                        throw new FileNotFoundException($"Dockerfile not found in build context: {buildContextPath}");
                    }

                    // Step 5: Reset tarStream and prepare build parameters
                    tarStream.Seek(0, SeekOrigin.Begin);

                    var buildParameters = new ImageBuildParameters
                    {
                        Dockerfile = dockerfilePath.Substring(tempDir.Length).TrimStart(Path.DirectorySeparatorChar).Replace("\\", "/"),
                        Tags = new List<string> { ImageName }
                    };

                    Console.WriteLine("Starting image build...");
                    var progress = new Progress<JSONMessage>(message =>
                    {
                        if (!string.IsNullOrWhiteSpace(message.Stream))
                        {
                            Console.WriteLine(message.Stream.Trim());
                        }
                    });

                    // Step 6: Build the Docker image
                    await client.Images.BuildImageFromDockerfileAsync(
                        buildParameters,
                        tarStream,
                        null,
                        new Dictionary<string, string>(),
                        progress,
                        CancellationToken.None
                    );

                    Console.WriteLine($"Image {ImageName} built successfully.");
                }

                // Step 7: Clean up temporary files
                if (File.Exists(tarballPath))
                {
                    File.Delete(tarballPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during image build: {ex.Message}");
                throw;
            }
        }


        private static void CreateTarball(string sourceDirectory, string tarballPath)
        {
            using (var tarStream = File.Create(tarballPath))
            using (var archive = new SharpCompress.Writers.Tar.TarWriter(tarStream, new SharpCompress.Writers.Tar.TarWriterOptions(SharpCompress.Common.CompressionType.None, true)))
            {
                foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    var entryPath = Path.GetRelativePath(sourceDirectory, filePath).Replace("\\", "/");
                    using var fileStream = File.OpenRead(filePath);
                    archive.Write(entryPath, fileStream, DateTime.UtcNow);
                }
            }
        }

        private DockerClient CreateDockerClient()
        {
            var config = new DockerClientConfiguration(new Uri(GetClientUri()));
            return config.CreateClient();

            string GetClientUri()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "npipe://./pipe/docker_engine";
                }
                else
                {
                    string podmanPath = $"/run/user/{geteuid()}/podman/podman.sock";
                    if (File.Exists(podmanPath))
                    {
                        return $"unix:{podmanPath}";
                    }

                    return "unix:/var/run/docker.sock";
                }
            }

            [DllImport("libc")]
            static extern uint geteuid();
        }

        [Fact]
        public void Test_SqlServerConnection_ShouldConnectSuccessfully()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Assert.True(connection.State == System.Data.ConnectionState.Open, "SQL Server connection should be open.");
            }
        }

        [Fact]
        public void Test_FullTextSearchInstallation_ShouldBeInstalled()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT SERVERPROPERTY('IsFullTextInstalled') AS IsFullTextInstalled;";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    Assert.NotNull(result);
                    Assert.Equal(1, Convert.ToInt32(result)); // FTS should be installed and return 1
                }
            }
        }

        [Fact]
        public void Test_CreateFullTextCatalog_ShouldSucceed()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string createDbQuery = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TestFTS') CREATE DATABASE TestFTS;";
                using (SqlCommand command = new SqlCommand(createDbQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.ChangeDatabase("TestFTS");

                string createTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'TestTable' AND xtype = 'U')
                    CREATE TABLE TestTable (
                        Id INT NOT NULL,
                        TextContent NVARCHAR(MAX),
                        CONSTRAINT PK_TestTable PRIMARY KEY (Id)
                    );

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TestTable_Id')
                    CREATE UNIQUE INDEX IX_TestTable_Id ON TestTable(Id);
                ";

                using (SqlCommand command = new SqlCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                string createFTSCatalogQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'TestFTS_Catalog')
                        BEGIN
                            CREATE FULLTEXT CATALOG TestFTS_Catalog AS DEFAULT;
                            CREATE FULLTEXT INDEX ON TestTable (TextContent) KEY INDEX PK_TestTable;
                        END";
                using (SqlCommand command = new SqlCommand(createFTSCatalogQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                string verifyFTSCatalogQuery = "SELECT COUNT(*) FROM sys.fulltext_catalogs WHERE name = 'TestFTS_Catalog';";
                using (SqlCommand command = new SqlCommand(verifyFTSCatalogQuery, connection))
                {
                    var result = command.ExecuteScalar();
                    Assert.NotNull(result);
                    Assert.Equal(1, Convert.ToInt32(result));
                }
            }
        }
    }
}
