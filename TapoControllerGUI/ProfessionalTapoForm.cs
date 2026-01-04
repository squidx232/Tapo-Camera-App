using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace TapoControllerGUI
{
    public partial class ProfessionalTapoForm : Form
    {
        private DataGridView dgvCameras = null!;
        private Button btnScanNetwork = null!;
        private Button btnStopScan = null!;
        private Button btnRefresh = null!;
        private TextBox txtSubnet = null!;
        private CancellationTokenSource? scanCancellation;
        private GroupBox grpNetworkScan = null!;
        private RichTextBox rtbLog = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblStatus = null!;
        private Panel pnlVideoStream = null!;
        private ComboBox cmbStreamType = null!;
        private Button btnStartStream = null!;
        private Button btnStopStream = null!;
        private Label lblStreamPlaceholder = null!;
        
        // PTZ Controls
        private GroupBox grpPTZControls = null!;
        private Button btnPTZUp = null!;
        private Button btnPTZDown = null!;
        private Button btnPTZLeft = null!;
        private Button btnPTZRight = null!;
        private Button btnPTZZoomIn = null!;
        private Button btnPTZZoomOut = null!;
        private Button btnPTZStop = null!;
        private ComboBox cmbPTZPresets = null!;
        private Button btnGotoPreset = null!;
        private OnvifPTZController? ptzController = null;

        private List<TapoCamera> discoveredCameras = new List<TapoCamera>();

        public ProfessionalTapoForm()
        {
            InitializeComponent();
            SetupProfessionalTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "Tapo Camera Management System";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(240, 240, 240);

            CreateMainLayout();
            CreateStatusBar();
        }

        private void SetupProfessionalTheme()
        {
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.BackColor = SystemColors.Control;
        }

        private void CreateMainLayout()
        {
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10),
                BackColor = SystemColors.Control
            };

            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

            var topPanel = CreateTopPanel();
            var bottomPanel = CreateBottomPanel();

            mainContainer.Controls.Add(topPanel, 0, 0);
            mainContainer.Controls.Add(bottomPanel, 0, 1);

            this.Controls.Add(mainContainer);
        }

        private Control CreateTopPanel()
        {
            grpNetworkScan = new GroupBox
            {
                Text = "Network Camera Discovery",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var toolbar = new FlowLayoutPanel
            {
                Height = 40,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 5)
            };

            btnScanNetwork = new Button
            {
                Text = "Scan Network",
                Width = 120,
                Height = 30,
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            btnScanNetwork.Click += BtnScanNetwork_Click;

            btnStopScan = new Button
            {
                Text = "Stop Scan",
                Width = 80,
                Height = 30,
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.FromArgb(220, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Enabled = false,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnStopScan.Click += BtnStopScan_Click;

            btnRefresh = new Button
            {
                Text = "Refresh",
                Width = 80,
                Height = 30,
                FlatStyle = FlatStyle.Standard,
                BackColor = SystemColors.Control,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnRefresh.Click += BtnRefresh_Click;

            var lblSubnet = new Label
            {
                Text = "Subnet:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(20, 7, 5, 0)
            };

            txtSubnet = new TextBox
            {
                Text = GetSubnet(GetLocalIPAddress()),
                Width = 100,
                Margin = new Padding(0, 5, 0, 0)
            };

            toolbar.Controls.Add(btnScanNetwork);
            toolbar.Controls.Add(btnStopScan);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(lblSubnet);
            toolbar.Controls.Add(txtSubnet);

            dgvCameras = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                GridColor = Color.LightGray,
                Font = new Font("Segoe UI", 8.5F)
            };

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "IPAddress",
                HeaderText = "IP Address",
                Width = 120
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Model",
                HeaderText = "Model",
                Width = 150
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 100
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LastSeen",
                HeaderText = "Last Seen",
                Width = 150
            });

            dgvCameras.SelectionChanged += DgvCameras_SelectionChanged;

            container.Controls.Add(toolbar, 0, 0);
            container.Controls.Add(dgvCameras, 0, 1);
            grpNetworkScan.Controls.Add(container);

            return grpNetworkScan;
        }

        private Control CreateBottomPanel()
        {
            var bottomContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };

            bottomContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            bottomContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            bottomContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            var logPanel = CreateLogPanel();
            var ptzPanel = CreatePTZPanel();
            var streamPanel = CreateStreamPanel();

            bottomContainer.Controls.Add(logPanel, 0, 0);
            bottomContainer.Controls.Add(ptzPanel, 1, 0);
            bottomContainer.Controls.Add(streamPanel, 2, 0);

            return bottomContainer;
        }

        private Control CreateLogPanel()
        {
            var grpLog = new GroupBox
            {
                Text = "Activity Log",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.Fixed3D
            };

            grpLog.Controls.Add(rtbLog);
            LogMessage("System initialized. Ready to scan for cameras.");
            
            return grpLog;
        }

        private Control CreatePTZPanel()
        {
            grpPTZControls = new GroupBox
            {
                Text = "PTZ Controls",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };

            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 3,
                Padding = new Padding(5)
            };

            // Configure rows and columns
            for (int i = 0; i < 5; i++)
                container.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            
            for (int i = 0; i < 3; i++)
                container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            // Row 0: Up button
            btnPTZUp = new Button
            {
                Text = "▲",
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(2)
            };
            btnPTZUp.Click += async (s, e) => await PTZMoveUp();
            container.Controls.Add(btnPTZUp, 1, 0);

            // Row 1: Left, Stop, Right buttons
            btnPTZLeft = new Button
            {
                Text = "◄",
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(2)
            };
            btnPTZLeft.Click += async (s, e) => await PTZMoveLeft();
            container.Controls.Add(btnPTZLeft, 0, 1);

            btnPTZStop = new Button
            {
                Text = "■",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(220, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(2)
            };
            btnPTZStop.Click += async (s, e) => await PTZStop();
            container.Controls.Add(btnPTZStop, 1, 1);

            btnPTZRight = new Button
            {
                Text = "►",
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(2)
            };
            btnPTZRight.Click += async (s, e) => await PTZMoveRight();
            container.Controls.Add(btnPTZRight, 2, 1);

            // Row 2: Down button
            btnPTZDown = new Button
            {
                Text = "▼",
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(2)
            };
            btnPTZDown.Click += async (s, e) => await PTZMoveDown();
            container.Controls.Add(btnPTZDown, 1, 2);

            // Row 3: Zoom buttons
            btnPTZZoomIn = new Button
            {
                Text = "Zoom +",
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(2)
            };
            btnPTZZoomIn.Click += async (s, e) => await PTZZoomIn();
            container.Controls.Add(btnPTZZoomIn, 0, 3);

            btnPTZZoomOut = new Button
            {
                Text = "Zoom -",
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(2)
            };
            btnPTZZoomOut.Click += async (s, e) => await PTZZoomOut();
            container.Controls.Add(btnPTZZoomOut, 2, 3);

            // Row 4: Presets
            var presetPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            cmbPTZPresets = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPTZPresets.Items.AddRange(new[] { "Preset 1", "Preset 2", "Preset 3", "Preset 4" });
            cmbPTZPresets.SelectedIndex = 0;

            btnGotoPreset = new Button
            {
                Text = "Go to Preset",
                Width = 150,
                Height = 25,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Margin = new Padding(0, 5, 0, 0)
            };
            btnGotoPreset.Click += async (s, e) => await PTZGotoPreset();

            presetPanel.Controls.Add(cmbPTZPresets);
            presetPanel.Controls.Add(btnGotoPreset);

            container.Controls.Add(presetPanel, 0, 4);
            container.SetColumnSpan(presetPanel, 3);

            grpPTZControls.Controls.Add(container);
            return grpPTZControls;
        }

        private Control CreateStreamPanel()
        {
            var grpCameraStream = new GroupBox
            {
                Text = "Live Camera Stream",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };

            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var streamControls = new FlowLayoutPanel
            {
                Height = 35,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 5)
            };

            streamControls.Controls.Add(new Label 
            { 
                Text = "Stream:", 
                AutoSize = true, 
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 7, 5, 0)
            });
            
            cmbStreamType = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 0)
            };
            cmbStreamType.Items.AddRange(new[] { "Auto-detect", "RTSP", "HTTP" });
            cmbStreamType.SelectedIndex = 0;
            streamControls.Controls.Add(cmbStreamType);

            pnlVideoStream = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.Fixed3D
            };

            lblStreamPlaceholder = new Label
            {
                Text = "Live Video Stream\n\nSelect a camera and click 'Start Stream'\nto view live video feed here",
                ForeColor = Color.White,
                BackColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };
            
            pnlVideoStream.Controls.Add(lblStreamPlaceholder);

            var streamButtons = new FlowLayoutPanel
            {
                Height = 40,
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 0)
            };

            btnStartStream = new Button
            {
                Text = "Start Stream",
                Width = 120,
                Height = 30,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Enabled = false
            };
            btnStartStream.Click += BtnStartStream_Click;

            btnStopStream = new Button
            {
                Text = "Stop Stream",
                Width = 120,
                Height = 30,
                BackColor = Color.FromArgb(220, 50, 50),
                ForeColor = Color.White,
                Margin = new Padding(10, 0, 0, 0),
                Enabled = false
            };
            btnStopStream.Click += BtnStopStream_Click;

            streamButtons.Controls.Add(btnStartStream);
            streamButtons.Controls.Add(btnStopStream);

            container.Controls.Add(streamControls, 0, 0);
            container.Controls.Add(pnlVideoStream, 0, 1);
            container.Controls.Add(streamButtons, 0, 2);

            grpCameraStream.Controls.Add(container);
            return grpCameraStream;
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip
            {
                BackColor = SystemColors.Control
            };

            lblStatus = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            statusStrip.Items.Add(lblStatus);
            this.Controls.Add(statusStrip);
        }

        // Event Handlers
        private async void BtnScanNetwork_Click(object? sender, EventArgs e)
        {
            btnScanNetwork.Enabled = false;
            btnStopScan.Enabled = true;
            btnScanNetwork.Text = "Scanning...";
            lblStatus.Text = "Scanning network for Tapo cameras...";
            
            try
            {
                LogMessage("Starting network scan for Tapo cameras...");
                discoveredCameras.Clear();
                dgvCameras.DataSource = null;
                
                scanCancellation = new CancellationTokenSource();
                await ScanForTapoCameras(scanCancellation.Token);
                
                if (!scanCancellation.Token.IsCancellationRequested)
                {
                    dgvCameras.DataSource = discoveredCameras;
                    lblStatus.Text = $"Scan complete - Found {discoveredCameras.Count} camera(s)";
                    LogMessage($"Network scan completed. Found {discoveredCameras.Count} Tapo camera(s).");
                }
                else
                {
                    lblStatus.Text = "Scan cancelled";
                    LogMessage("Network scan cancelled by user.");
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Scan cancelled";
                LogMessage("Network scan cancelled by user.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error during network scan: {ex.Message}");
                lblStatus.Text = "Scan failed";
            }
            finally
            {
                btnScanNetwork.Enabled = true;
                btnStopScan.Enabled = false;
                btnScanNetwork.Text = "Scan Network";
                scanCancellation?.Dispose();
                scanCancellation = null;
            }
        }

        private void BtnStopScan_Click(object? sender, EventArgs e)
        {
            scanCancellation?.Cancel();
            btnStopScan.Enabled = false;
            LogMessage("Stopping network scan...");
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            dgvCameras.Refresh();
            LogMessage("Camera list refreshed.");
        }

        private void DgvCameras_SelectionChanged(object? sender, EventArgs e)
        {
            bool hasSelection = dgvCameras.SelectedRows.Count > 0;
            btnStartStream.Enabled = hasSelection;
            grpPTZControls.Enabled = hasSelection;
            
            if (hasSelection && dgvCameras.SelectedRows[0].DataBoundItem is TapoCamera camera)
            {
                LogMessage($"Selected camera: {camera.IPAddress} ({camera.Model})");
                InitializePTZController(camera.IPAddress);
            }
            else
            {
                ptzController?.Disconnect();
                ptzController = null;
            }
        }

        private void BtnStartStream_Click(object? sender, EventArgs e)
        {
            if (dgvCameras.SelectedRows.Count > 0)
            {
                var camera = dgvCameras.SelectedRows[0].DataBoundItem as TapoCamera;
                if (camera != null)
                {
                    lblStreamPlaceholder.Text = $"Connecting to camera stream...\n{camera.IPAddress}";
                    LogMessage($"Starting stream from {camera.IPAddress}");
                    btnStartStream.Enabled = false;
                    btnStopStream.Enabled = true;
                }
            }
        }

        private void BtnStopStream_Click(object? sender, EventArgs e)
        {
            lblStreamPlaceholder.Text = "Live Video Stream\n\nSelect a camera and click 'Start Stream'\nto view live video feed here";
            LogMessage("Stream stopped.");
            btnStartStream.Enabled = true;
            btnStopStream.Enabled = false;
        }

        // Core Functionality
        private async Task ScanForTapoCameras(CancellationToken cancellationToken = default)
        {
            var subnet = txtSubnet.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(subnet) || subnet.Split('.').Length != 3)
            {
                LogMessage("Invalid subnet format. Using auto-detected subnet.");
                subnet = GetSubnet(GetLocalIPAddress());
                txtSubnet.Text = subnet;
            }
            
            LogMessage($"Scanning subnet: {subnet}.0/24");
            
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(100);
            
            for (int i = 1; i <= 254; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var ip = $"{subnet}.{i}";
                
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await ScanSingleIP(ip, cancellationToken);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }
            
            await Task.WhenAll(tasks);
        }

        private async Task ScanSingleIP(string ip, CancellationToken cancellationToken = default)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 200);
                bool isReachable = reply.Status == IPStatus.Success;
                
                if (isReachable && !cancellationToken.IsCancellationRequested)
                {
                    if (await IsTapoCamera(ip, cancellationToken))
                    {
                        var camera = new TapoCamera
                        {
                            IPAddress = ip,
                            Model = await DetectCameraModel(ip, cancellationToken),
                            Status = "Online",
                            LastSeen = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        
                        discoveredCameras.Add(camera);
                        
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Invoke(new Action(() =>
                            {
                                LogMessage($"Found Tapo camera at {ip} - Model: {camera.Model}");
                                dgvCameras.DataSource = null;
                                dgvCameras.DataSource = discoveredCameras;
                            }));
                        }
                    }
                }
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
            }
        }

        private async Task<bool> IsTapoCamera(string ip, CancellationToken cancellationToken = default)
        {
            try
            {
                // Configure HttpClient to accept self-signed certificates (Tapo cameras use self-signed certs)
                var handler = new System.Net.Http.HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                
                using var client = new System.Net.Http.HttpClient(handler);
                client.Timeout = TimeSpan.FromMilliseconds(1000);
                
                // Check HTTPS first (port 443) - Tapo cameras typically use HTTPS
                try
                {
                    var response = await client.GetAsync($"https://{ip}");
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Look for Tapo/TP-Link signatures
                    if (content.Contains("tapo", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("tp-link", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("SMART.IPCAMERA", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("ipcamera", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    
                    // If we got any response on HTTPS 443, it's likely a camera
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Try HTTP if HTTPS fails
                    try
                    {
                        var response = await client.GetAsync($"http://{ip}");
                        var content = await response.Content.ReadAsStringAsync();
                        
                        if (content.Contains("tapo", StringComparison.OrdinalIgnoreCase) ||
                            content.Contains("tp-link", StringComparison.OrdinalIgnoreCase) ||
                            content.Contains("ipcamera", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch { }
                }
                
                // Check if RTSP port (554) is open - Tapo cameras have RTSP
                try
                {
                    using var tcpClient = new System.Net.Sockets.TcpClient();
                    await tcpClient.ConnectAsync(ip, 554).WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
                    if (tcpClient.Connected)
                    {
                        return true; // Has RTSP port, likely a camera
                    }
                }
                catch { }
            }
            catch
            {
            }
            
            return false;
        }

        private async Task<string> DetectCameraModel(string ip, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return "Tapo Camera";
        }

        private void LogMessage(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.AppendText($"[{timestamp}] {message}\n");
            rtbLog.ScrollToCaret();
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "192.168.1.1";
        }

        private string GetSubnet(string ipAddress)
        {
            var parts = ipAddress.Split('.');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            return "192.168.1";
        }

        // PTZ Control Methods
        private void InitializePTZController(string ipAddress)
        {
            try
            {
                // Default ONVIF credentials - user should configure these
                ptzController = new OnvifPTZController(ipAddress, "admin", "admin");
                LogMessage($"PTZ controller initialized for {ipAddress}");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to initialize PTZ controller: {ex.Message}");
            }
        }

        private async Task PTZMoveUp()
        {
            if (ptzController == null) return;
            try
            {
                if (!await ptzController.ConnectAsync())
                {
                    LogMessage("Failed to connect to camera for PTZ control");
                    return;
                }
                await ptzController.MoveUpAsync(0.5f);
                LogMessage("PTZ: Moving Up");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Up error: {ex.Message}");
            }
        }

        private async Task PTZMoveDown()
        {
            if (ptzController == null) return;
            try
            {
                if (!await ptzController.ConnectAsync())
                {
                    LogMessage("Failed to connect to camera for PTZ control");
                    return;
                }
                await ptzController.MoveDownAsync(0.5f);
                LogMessage("PTZ: Moving Down");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Down error: {ex.Message}");
            }
        }

        private async Task PTZMoveLeft()
        {
            if (ptzController == null) return;
            try
            {
                if (!await ptzController.ConnectAsync())
                {
                    LogMessage("Failed to connect to camera for PTZ control");
                    return;
                }
                await ptzController.MoveLeftAsync(0.5f);
                LogMessage("PTZ: Moving Left");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Left error: {ex.Message}");
            }
        }

        private async Task PTZMoveRight()
        {
            if (ptzController == null) return;
            try
            {
                if (!await ptzController.ConnectAsync())
                {
                    LogMessage("Failed to connect to camera for PTZ control");
                    return;
                }
                await ptzController.MoveRightAsync(0.5f);
                LogMessage("PTZ: Moving Right");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Right error: {ex.Message}");
            }
        }

        private async Task PTZZoomIn()
        {
            if (ptzController == null) return;
            try
            {
                if (!await ptzController.ConnectAsync())
                {
                    LogMessage("Failed to connect to camera for PTZ control");
                    return;
                }
                await ptzController.ZoomInAsync(0.5f);
                LogMessage("PTZ: Zooming In");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Zoom In error: {ex.Message}");
            }
        }

        private async Task PTZZoomOut()
        {
            if (ptzController == null) return;
            try
            {
                if (!await ptzController.ConnectAsync())
                {
                    LogMessage("Failed to connect to camera for PTZ control");
                    return;
                }
                await ptzController.ZoomOutAsync(0.5f);
                LogMessage("PTZ: Zooming Out");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Zoom Out error: {ex.Message}");
            }
        }

        private async Task PTZStop()
        {
            if (ptzController == null) return;
            try
            {
                await ptzController.StopAsync();
                LogMessage("PTZ: Stopped");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Stop error: {ex.Message}");
            }
        }

        private async Task PTZGotoPreset()
        {
            if (ptzController == null) return;
            try
            {
                if (!await ptzController.ConnectAsync())
                {
                    LogMessage("Failed to connect to camera for PTZ control");
                    return;
                }
                var presetIndex = cmbPTZPresets.SelectedIndex;
                var success = await ptzController.GotoPresetAsync(presetIndex);
                if (success)
                    LogMessage($"PTZ: Going to Preset {presetIndex + 1}");
                else
                    LogMessage($"PTZ: Failed to go to Preset {presetIndex + 1}");
            }
            catch (Exception ex)
            {
                LogMessage($"PTZ Preset error: {ex.Message}");
            }
        }
    }

    public class TapoCamera
    {
        public string IPAddress { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastSeen { get; set; } = string.Empty;
    }
}
