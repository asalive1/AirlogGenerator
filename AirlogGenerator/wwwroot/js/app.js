// Detect folder picker support
const supportsFolderPicker = false;
const browserInfo = navigator.userAgent;

let currentLogLevel = "DEBUG"; // default until system.json loads
let systemConfigCache = null;

appendLog(`[INFO] Browser detected: ${browserInfo}`);
if (!supportsFolderPicker) {
    appendLog("[INFO] Folder picker not supported in this browser. Manual path entry only.");
} else {
    appendLog("[INFO] Folder picker supported. Browse button enabled.");
}

function appendLog(line) {
    const logWindow = document.getElementById("log-window");
    const ts = new Date().toISOString();
    if (!shouldDisplayLog(line)) return;
    logWindow.textContent += `[${ts}] ${line}\n`;
    logWindow.scrollTop = logWindow.scrollHeight;
}

function mapQueryType(num) {
    switch (num) {
        case 0: return "Standard";
        case 1: return "withPartnerID";
        case 2: return "withRotIDonly";
        case 3: return "withRotMAID";
        case 4: return "withRotandMAID";
        case 5: return "RotatorMAIDpid";
        default: return "Standard";
    }
}

document.addEventListener("DOMContentLoaded", () => {
    appendLog("[INFO] AIRLOG GENERATOR UI loaded.");

    fetch("/api/status")
        .then(r => r.json())
        .then(data => {
            document.getElementById("app-version").innerText = "v" + data.version;
            document.getElementById("server-ip").value = data.defaultServerIp;
            appendLog("[SUCCESS] Connected to backend.");
        })
        .catch(err => {
            appendLog("[ERROR] Cannot reach backend: " + err);
        });

    fetch("/api/server/version")
        .then(r => r.json())
        .then(data => {
            document.getElementById("server-status").textContent = "Server: " + data.version;
            //appendLog("[INFO] WO AFR version: " + data.version);
        });

    // Start log stream and load system settings after DOM is ready
    startLogStream();
    loadSystemConfig();

    // Wire save button
    const sysSaveBtn = document.getElementById("sys-save-btn");
    if (sysSaveBtn) {
        sysSaveBtn.addEventListener("click", saveSystemConfig);
    }
});

function startLogStream() {
    const logWindow = document.getElementById("log-window");

    const evtSource = new EventSource("/api/log/live");

    evtSource.onmessage = (event) => {
        if (shouldDisplayLog(event.data)) {
            logWindow.textContent += event.data + "\n";
        }
        logWindow.scrollTop = logWindow.scrollHeight;
    };

    evtSource.onerror = () => {
        logWindow.textContent += "[ERROR] Lost connection to log stream.\n";
    };
}

function shouldDisplayLog(line) {
    const level = currentLogLevel;

    if (level === "DEBUG") return true;

    if (level === "INFO") {
        return !line.includes("[DEBUG]");
    }

    if (level === "WARN") {
        return line.includes("[WARN]") ||
            line.includes("[ERROR]") ||
            line.includes("[SUCCESS]");
    }

    if (level === "ERROR") {
        return line.includes("[ERROR]");
    }

    return true;
}

async function loadSystemConfig() {
    try {
        const res = await fetch("/api/system");
        const cfg = await res.json();
        systemConfigCache = cfg;

        document.getElementById("sys-webport").value = cfg.webPort;
        document.getElementById("sys-scheduler-interval").value = cfg.schedulerIntervalMinutes;
        currentLogLevel = cfg.logging.level;
        document.getElementById("sys-log-level").value = cfg.logging.level;

        document.getElementById("sys-log-retention").value = cfg.logging.retentionDays;
        document.getElementById("sys-log-maxsize").value = cfg.logging.maxSizeMb;

        // Also keep Central Server IP in sync
        document.getElementById("server-ip").value = cfg.defaultServerIp;

    } catch (err) {
        console.error("Failed to load system.json", err);
        showError("Failed to load system settings");
    }
}

async function saveSystemConfig() {
    const cached = systemConfigCache ?? {};

    const cfg = {
        enableWebUi: cached.enableWebUi ?? true,
        webPort: parseInt(document.getElementById("sys-webport").value),
        defaultServerIp: document.getElementById("server-ip").value,
        defaultServerHost: cached.defaultServerHost ?? "",
        defaultDatabaseName: cached.defaultDatabaseName ?? "woar_server",
        defaultServerUser: cached.defaultServerUser ?? "",
        defaultServerPassword: cached.defaultServerPassword ?? "",
        defaultServerPasswordSecret: cached.defaultServerPasswordSecret ?? "",
        defaultServerPasswordSecretRegion: cached.defaultServerPasswordSecretRegion ?? "",
        schedulerIntervalMinutes: parseInt(document.getElementById("sys-scheduler-interval").value),
        logging: {
            level: document.getElementById("sys-log-level").value,
            retentionDays: parseInt(document.getElementById("sys-log-retention").value),
            maxSizeMb: parseInt(document.getElementById("sys-log-maxsize").value)
        }
    };

    try {
        const res = await fetch("/api/system", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(cfg)
        });

        const result = await res.json();

        if (result.success) {
            systemConfigCache = cfg;
            showSuccess("System settings saved");
            //appendLog("[SUCCESS] System settings saved.");
        } else {
            showError("Failed to save system settings");
            appendLog("[ERROR] Failed to save system settings.");
        }
        currentLogLevel = cfg.logging.level;
    } catch (err) {
        console.error("Save failed", err);
        showError("Save failed");
        appendLog("[ERROR] System settings save failed: " + err);
    }
}

document.getElementById("connect-btn").addEventListener("click", () => {
    const host = document.getElementById("server-ip").value;

    const payload = {
        host,
        database: "woar_server",
        user: "",
        password: ""
    };

    fetch("/api/db/connect", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                appendLog("[SUCCESS] Connected to database.");

                // Load station list
                loadStations();

                // Fetch PostgreSQL version
                fetch("/api/db/version")
                    .then(r => r.json())
                    .then(v => {
                        document.getElementById("db-status").textContent = "DB: " + v.version;
                        appendLog("[INFO] PostgreSQL version: " + v.version);
                    });

                // Update backend with new server IP
                fetch("/api/server/setip", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ ip: host })
                })
                    .then(() => fetch("/api/server/version"))
                    .then(r => r.json())
                    .then(data => {
                        document.getElementById("server-status").textContent = "WO AFR Version: " + data.version;
                        appendLog("[INFO] WO AFR version: " + data.version);
                    });

            } else {
                appendLog("[ERROR] Failed to connect to database.");
            }
        });
});
function loadStations() {
    appendLog("[DEBUG] loadStations() called.");
    appendLog("[INFO] Loading station list...");

    fetch("/api/stations")
        .then(r => r.json())
        .then(stations => {
            appendLog(`[SUCCESS] Loaded ${stations.length} stations.`);

            const list = document.getElementById("station-list");
            list.innerHTML = "";

            stations.forEach(station => {
                const div = document.createElement("div");

                const displayOffset = formatOffset(station.timezoneOffset);

                div.textContent = `${station.stationName} (Offset ${displayOffset} Hours from Central Server)`;
                div.classList.add("station-item");

                div.addEventListener("click", () => {
                    appendLog("[INFO] Selected station: " + station.stationName);
                    selectStation(station.stationName, station.timezoneOffset);
                });

                list.appendChild(div);
            });
        })
        .catch(err => {
            appendLog("[ERROR] Failed to load stations: " + err);
        });
}
function formatOffset(offset) {
    // If it's exactly zero (0 or 0.0), return "0"
    if (offset === 0) return "0";

    // If it's a whole number like 3.0 or -4.0, drop the .0
    if (Number.isInteger(offset)) return offset.toString();

    // Otherwise preserve the decimal (e.g., -3.5, 0.5)
    return offset.toString();
}

async function selectStation(name, offset) {
    const r = await fetch(`/api/stations/${name}`);

    let cfg;

    if (r.status === 404) {
        cfg = {
            stationName: name,
            timezoneOffset: offset,
            enableTimezoneCalc: true,
            destinations: [],
            schedule: []
        };
    } else {
        cfg = await r.json();
        cfg.stationName = name;
        cfg.timezoneOffset = offset;
    }

    // Store config
    window.currentStation = cfg;

    // Render UI
    renderStationEditor(cfg);
}


function renderStationEditor(cfg) {
    const panel = document.getElementById("station-details");

    panel.innerHTML = `
        <h3>${cfg.stationName}</h3>

        <label>
            Timezone Offset:
            <input type="text" id="tz-offset" value="${formatOffset(cfg.timezoneOffset)}" readonly>
        </label>

        <label style="margin-left: 10px;">
            <input type="checkbox" id="tz-enable" ${cfg.enableTimezoneCalc ? "checked" : ""}>
            Enable Calculation
        </label>

        <h4>On Demand Execution</h4>
<div id="ondemand-section">
    <div id="ondemand-calendar"></div>

        <div style="margin-top: 10px;">
        <label for="ondemand-query-select">Query Type:</label>
        <select id="ondemand-query-select">
            <option value="Standard">Standard</option>
            <option value="withPartnerID">Partner ID</option>
            <option value="withRotIDonly">Rotator Only</option>
            <option value="withRotMAID">Rotator + MAID</option>
            <option value="withRotandMAID">Rotator + Partner ID</option>
            <option value="RotatorMAIDpid">Rotator + MAID + Partner ID</option>
        </select>
    </div>

    <div style="margin-top: 10px;">
        <label>Destination:</label>
        <input type="text" id="ondemand-dest" placeholder="Enter or browse destination">
       
    </div>

    <!-- ⭐ Split Execute Button -->
    <div id="ondemand-execute-row" style="margin-top: 10px; display:flex; align-items:center; gap:4px; position:relative;">
        <button id="ondemand-execute-main">Execute</button>
        <button id="ondemand-execute-menu-toggle" title="Choose action">▼</button>

        <!-- ⭐ Dropdown Menu -->
        <div id="ondemand-execute-menu"
             style="
                position: absolute;
                top: 38px;
                left: 0;
                display: none;
                background: white;
                border: 1px solid #ccc;
                box-shadow: 0 2px 4px rgba(0,0,0,0.2);
                z-index: 1000;
             ">
            <div class="ondemand-menu-item" data-mode="save"     style="padding:6px 12px; cursor:pointer;">Save / Write</div>
            <div class="ondemand-menu-item" data-mode="preview"  style="padding:6px 12px; cursor:pointer;">Preview</div>
            <div class="ondemand-menu-item" data-mode="download" style="padding:6px 12px; cursor:pointer;">Download</div>
        </div>
    </div>

    <!-- ⭐ Inline Preview Panel -->
    <div id="ondemand-preview-container" style="display:none; margin-top:20px;">
        <h4>Preview: <span id="ondemand-preview-filename"></span></h4>

        <div id="ondemand-preview-wrapper"
             style="
                border: 1px solid #ccc;
                padding: 10px;
                max-height: 300px;
                overflow: auto;
                white-space: pre;
                font-family: monospace;
                background: #f8f8f8;
             ">
            <pre id="ondemand-preview" style="margin:0;"></pre>
        </div>

        <button id="ondemand-preview-close" style="margin-top:10px;">Close Preview</button>
    </div>
</div>

        <h4>Schedule</h4>
        <div id="schedule-list"></div>
        <button id="add-sched-btn">Add Schedule Entry</button>

        <button id="save-btn" style="margin-top: 20px;">Save Station Config</button>
    `;

    document.getElementById("save-btn").addEventListener("click", () => {
        const name = cfg.stationName;

        cfg.enableTimezoneCalc = document.getElementById("tz-enable").checked;

        cfg.destinations = Array.from(document.querySelectorAll("#dest-list input"))
            .map(i => i.value);

        cfg.host = document.getElementById("server-ip").value;

        console.log("Saving station:", name);
        console.log("Saving config:", cfg);

        fetch(`/api/stations/${name}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(cfg, null, 2)
        })
            .then(() => appendLog(`[SUCCESS] Saved configuration for ${name}`))
            .catch(err => appendLog(`[ERROR] Failed to save: ${err}`));
    });

    document.getElementById("tz-enable").addEventListener("change", () => {
        window.currentStation.enableTimezoneCalc = document.getElementById("tz-enable").checked;
    });

    renderOnDemandCalendar();
    const browseBtn = document.getElementById("ondemand-browse");
    if (browseBtn) {
        browseBtn.onclick = () => {
            const picker = document.createElement("input");
            picker.type = "file";
            picker.webkitdirectory = true;
            picker.style.display = "none";

            picker.addEventListener("change", () => {
                if (picker.files.length > 0) {
                    const fullPath = picker.files[0].webkitRelativePath;
                    const folder = fullPath.split("/")[0];
                    document.getElementById("ondemand-dest").value = folder;
                    appendLog(`[INFO] On Demand destination set to: ${folder}`);
                }
            });

            document.body.appendChild(picker);
            picker.click();
            picker.addEventListener("blur", () => picker.remove());
        };
    }

    // ===============================
    // ⭐ Split Execute Button Wiring
    // ===============================

    // Track selected mode (default = preview)
    let onDemandMode = "preview";

    // Set initial button text to match default mode
    const executeMainBtn = document.getElementById("ondemand-execute-main");
    executeMainBtn.textContent = "Preview";

    // Toggle dropdown menu
    const menuToggle = document.getElementById("ondemand-execute-menu-toggle");
    const menu = document.getElementById("ondemand-execute-menu");

    menuToggle.onclick = (e) => {
        e.stopPropagation();
        menu.style.display = menu.style.display === "block" ? "none" : "block";
    };

    // Close menu when clicking outside
    document.addEventListener("click", () => {
        menu.style.display = "none";
    });

    // Handle menu item selection
    document.querySelectorAll(".ondemand-menu-item").forEach(item => {
        item.addEventListener("click", () => {
            onDemandMode = item.dataset.mode;   // save | preview | download
            menu.style.display = "none";

            // Update main button label to reflect selection
            if (onDemandMode === "save") {
                executeMainBtn.textContent = "Save / Write";
            } else if (onDemandMode === "preview") {
                executeMainBtn.textContent = "Preview";
            } else if (onDemandMode === "download") {
                executeMainBtn.textContent = "Download";
            }

            appendLog(`[INFO] On Demand mode set to: ${onDemandMode}`);
        });
    });

    // Main Execute button
    document.getElementById("ondemand-execute-main").onclick = async () => {
        const dates = Array.from(window.onDemandSelectedDates || []);
        const dest = document.getElementById("ondemand-dest").value;
        const selectedQuery = document.getElementById("ondemand-query-select").value || "Standard";

        if (dates.length === 0) {
            appendLog("[WARN] No dates selected for On Demand execution.");
            return;
        }

        if (onDemandMode === "save" && (!dest || dest.trim() === "")) {
            appendLog("[WARN] No destination selected for Save/Write.");
            return;
        }

        const payload = {
            station: cfg.stationName,
            dates,
            destination: dest,
            queryType: selectedQuery
        };

        // ===============================
        // ⭐ SAVE / WRITE MODE
        // ===============================
        if (onDemandMode === "save") {
            appendLog(`[INFO] Executing Save/Write for ${dates.length} date(s)...`);

            fetch("/api/ondemand", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            })
                .then(r => r.json())
                .then(data => {
                    if (data.success) {
                        appendLog(`[SUCCESS] Files written: ${data.files.length}`);
                    } else {
                        appendLog("[ERROR] Save/Write failed.");
                    }
                })
                .catch(err => appendLog(`[ERROR] Save/Write failed: ${err}`));

            return;
        }

        // ===============================
        // ⭐ PREVIEW MODE
        // ===============================
        if (onDemandMode === "preview") {
            appendLog(`[INFO] Generating preview for ${dates.length} date(s)...`);

            const res = await fetch("/api/ondemand/preview", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const data = await res.json();

            if (!data.success) {
                appendLog("[WARN] No preview data returned.");
                return;
            }

            // Fill preview panel
            document.getElementById("ondemand-preview-filename").textContent = data.fileName;
            document.getElementById("ondemand-preview").textContent = data.lines.join("\n");

            // Show preview panel
            document.getElementById("ondemand-preview-container").style.display = "block";

            appendLog(`[SUCCESS] Preview generated (${data.lines.length} lines).`);
            return;
        }

        // ===============================
        // ⭐ DOWNLOAD MODE
        // ===============================
        if (onDemandMode === "download") {
            appendLog(`[INFO] Downloading AIR log for ${dates.length} date(s)...`);

            const res = await fetch("/api/ondemand/download", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                appendLog("[ERROR] Download failed.");
                return;
            }

            const blob = await res.blob();
            const url = URL.createObjectURL(blob);

            // Extract filename from header
            const header = res.headers.get("Content-Disposition");
            let filename = "ondemand.air";

            if (header && header.toLowerCase().includes("filename")) {
                // Try to find filename= or filename*= part
                // Example: attachment; filename="WOXY-260101.air"; filename*=UTF-8''WOXY-260101.air
                const match = header.match(/filename\*?=([^;]+)/i);
                if (match && match[1]) {
                    let raw = match[1].trim();

                    // Strip UTF-8'' prefix if present
                    raw = raw.replace(/^UTF-8''/i, "");

                    // Strip surrounding quotes if present
                    raw = raw.replace(/^"/, "").replace(/"$/, "");

                    filename = raw;
                }
            }

            // Trigger download
            const a = document.createElement("a");
            a.href = url;
            a.download = filename;
            a.click();
            URL.revokeObjectURL(url);

            appendLog(`[SUCCESS] Downloaded file: ${filename}`);
            return;
        }
    };

    // ===============================
    // ⭐ Close Preview Button
    // ===============================
    document.getElementById("ondemand-preview-close").onclick = () => {
        document.getElementById("ondemand-preview-container").style.display = "none";
        appendLog("[INFO] Preview closed.");
    };

    // Set checkbox state
    document.getElementById("tz-enable").checked = cfg.enableTimezoneCalc;

    // 🔥 These two lines MUST pass the loaded lists
    //renderDestinations(cfg.destinations);
    renderSchedule(cfg.schedule);
}

function renderOnDemandCalendar() {
    const container = document.getElementById("ondemand-calendar");
    container.innerHTML = "";

    const today = new Date();
    const now = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const maxPastDate = new Date(now);
    maxPastDate.setDate(maxPastDate.getDate() - 30);

    // Track current calendar month
    if (!window.calendarState) {
        window.calendarState = {
            year: now.getFullYear(),
            month: now.getMonth()
        };
    }

    const { year, month } = window.calendarState;
    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const selectedDates = window.onDemandSelectedDates || new Set();
    window.onDemandSelectedDates = selectedDates;

    // Header with month and arrows
    const header = document.createElement("div");
    header.style.display = "flex";
    header.style.justifyContent = "center";
    header.style.alignItems = "center";
    header.style.gap = "10px";
    header.style.marginBottom = "10px";

    const prevBtn = document.createElement("button");
    prevBtn.textContent = "<";
    prevBtn.disabled = new Date(year, month, 1) <= maxPastDate;
    prevBtn.onclick = () => {
        if (month === 0) {
            window.calendarState.year -= 1;
            window.calendarState.month = 11;
        } else {
            window.calendarState.month -= 1;
        }
        renderOnDemandCalendar();
    };

    const nextBtn = document.createElement("button");
    nextBtn.textContent = ">";
    nextBtn.disabled = month === now.getMonth() && year === now.getFullYear();
    nextBtn.onclick = () => {
        if (month === 11) {
            window.calendarState.year += 1;
            window.calendarState.month = 0;
        } else {
            window.calendarState.month += 1;
        }
        renderOnDemandCalendar();
    };

    const monthLabel = document.createElement("span");
    monthLabel.textContent = `${new Date(year, month).toLocaleString("default", { month: "long" })} ${year}`;
    monthLabel.style.fontWeight = "bold";

    header.appendChild(prevBtn);
    header.appendChild(monthLabel);
    header.appendChild(nextBtn);
    container.appendChild(header);

    // Calendar grid
    const grid = document.createElement("div");
    grid.style.display = "grid";
    grid.style.gridTemplateColumns = "repeat(7, 1fr)";
    grid.style.gap = "5px";

    for (let i = 0; i < firstDay; i++) {
        grid.appendChild(document.createElement("div"));
    }

    for (let d = 1; d <= daysInMonth; d++) {
        const dateObj = new Date(year, month, d);
        const mm = String(month + 1).padStart(2, "0");
        const dd = String(d).padStart(2, "0");
        const dateStr = `${year}-${mm}-${dd}`;
        const cell = document.createElement("div");
        cell.textContent = d;
        cell.classList.add("calendar-cell");
        cell.dataset.date = dateStr;

        const isTooOld = dateObj < maxPastDate;
        const isFuture = dateObj > now;

        if (isTooOld || isFuture) {
            cell.classList.add("disabled");
        } else {
            cell.addEventListener("click", (e) => {
                if (e.ctrlKey || e.metaKey || e.shiftKey) {
                    cell.classList.toggle("selected");
                    if (selectedDates.has(dateStr)) {
                        selectedDates.delete(dateStr);
                    } else {
                        selectedDates.add(dateStr);
                    }
                } else {
                    grid.querySelectorAll(".calendar-cell").forEach(c => c.classList.remove("selected"));
                    selectedDates.clear();
                    cell.classList.add("selected");
                    selectedDates.add(dateStr);
                }
            });

            if (selectedDates.has(dateStr)) {
                cell.classList.add("selected");
            }
        }

        grid.appendChild(cell);
    }

    container.appendChild(grid);
}

function renderDestinations(list) {
    const container = document.getElementById("dest-list");

    // Capture current values before re-rendering
    const currentValues = Array.from(container.querySelectorAll("input"))
        .map(i => i.value);

    // Update list with current values
    for (let i = 0; i < currentValues.length; i++) {
        list[i] = currentValues[i];
    }

    container.innerHTML = "";

    list.forEach((dest, index) => {
        const row = document.createElement("div");
        row.classList.add("dest-row");

        let html = `
            <input type="text" value="${dest}" data-index="${index}" class="dest-input">
        `;

        if (supportsFolderPicker) {
            html += `
                <button type="button" class="browse-dest" data-index="${index}">Browse</button>
            `;
        }

        html += `
            <button type="button" data-index="${index}" class="remove-dest">X</button>
        `;

        row.innerHTML = html;
        container.appendChild(row);
    });

    // Remove destination
    document.querySelectorAll(".remove-dest").forEach(btn => {
        btn.addEventListener("click", () => {
            const idx = btn.dataset.index;
            list.splice(idx, 1);
            renderDestinations(list);
        });
    });

    // Add destination
    document.getElementById("add-dest-btn").onclick = () => {
        list.push("");
        renderDestinations(list);
    };

    // Wire up Browse buttons only if supported
    if (supportsFolderPicker) {
        wireBrowseButtons(list);
    }
}
function wireBrowseButtons(list) {
    document.querySelectorAll(".browse-dest").forEach(btn => {
        btn.addEventListener("click", () => {
            const idx = btn.dataset.index;

            // Create a hidden folder picker input
            const picker = document.createElement("input");
            picker.type = "file";
            picker.webkitdirectory = true;   // REQUIRED for folder picking
            picker.style.display = "none";

            picker.addEventListener("change", () => {
                if (picker.files.length > 0) {
                    const fullPath = picker.files[0].webkitRelativePath;
                    const folder = fullPath.split("/")[0];

                    if (folder) {
                        appendLog(`[INFO] Folder selected: ${folder}`);
                        list[idx] = folder;
                        renderDestinations(list);
                    }
                }
            });

            // Trigger the picker
            document.body.appendChild(picker);
            picker.click();

            // Clean up after use
            picker.addEventListener("blur", () => {
                picker.remove();
            });
        });
    });
}

function renderDestinationsForSchedule(entry, schedIndex) {
    const container = document.getElementById(`sched-dest-list-${schedIndex}`);
    container.innerHTML = "";

    entry.destinations = entry.destinations || [];

    entry.destinations.forEach((dest, destIndex) => {
        const row = document.createElement("div");
        row.classList.add("dest-row");

        let html = `
            <input type="text" value="${dest}" 
                   data-sched="${schedIndex}" 
                   data-dest="${destIndex}" 
                   class="sched-dest-input">
        `;

        if (supportsFolderPicker) {
            html += `
                <button type="button" 
                        class="sched-browse-dest" 
                        data-sched="${schedIndex}" 
                        data-dest="${destIndex}">
                    Browse
                </button>
            `;
        }

        html += `
            <button type="button" 
                    class="sched-remove-dest" 
                    data-sched="${schedIndex}" 
                    data-dest="${destIndex}">
                X
            </button>
        `;

        row.innerHTML = html;
        container.appendChild(row);
    });

    // 🔥 FIX #1 — Update destination array when typing
    container.querySelectorAll(".sched-dest-input").forEach(inp => {
        inp.addEventListener("input", () => {
            const s = inp.dataset.sched;
            const d = inp.dataset.dest;
            window.currentStation.schedule[s].destinations[d] = inp.value;
        });
    });

    // Remove destination
    container.querySelectorAll(".sched-remove-dest").forEach(btn => {
        btn.addEventListener("click", () => {
            const s = btn.dataset.sched;
            const d = btn.dataset.dest;
            window.currentStation.schedule[s].destinations.splice(d, 1);
            renderSchedule(window.currentStation.schedule);
        });
    });

    // Browse buttons
    if (supportsFolderPicker) {
        container.querySelectorAll(".sched-browse-dest").forEach(btn => {
            btn.addEventListener("click", () => {
                const s = btn.dataset.sched;
                const d = btn.dataset.dest;

                const picker = document.createElement("input");
                picker.type = "file";
                picker.webkitdirectory = true;
                picker.style.display = "none";

                picker.addEventListener("change", () => {
                    if (picker.files.length > 0) {
                        const fullPath = picker.files[0].webkitRelativePath;
                        const folder = fullPath.split("/")[0];

                        if (folder) {
                            appendLog(`[INFO] Folder selected: ${folder}`);
                            window.currentStation.schedule[s].destinations[d] = folder;
                            renderSchedule(window.currentStation.schedule);
                        }
                    }
                });

                document.body.appendChild(picker);
                picker.click();
                picker.addEventListener("blur", () => picker.remove());
            });
        });
    }

    // Add destination
    document.getElementById(`sched-add-dest-${schedIndex}`).onclick = () => {
        entry.destinations.push("");
        renderSchedule(window.currentStation.schedule);
    };
}

// Setting Default Schedule Entry
const DEFAULT_SCHEDULE_ENTRY = {
    mode: "daily",
    interval: 1,
    time: "00:15",
    day: null,
    dayOfMonth: null,
    destinations: [],
    queryType: 0
};
function renderSchedule(list) {
    const scrollBox = document.getElementById("station-details");   // ⭐ correct scroll container
    const container = document.getElementById("schedule-list");

    // ⭐ Save scroll position BEFORE clearing
    const previousScroll = scrollBox.scrollTop;

    // ⭐ Detect if user was near the bottom
    const isNearBottom =
        scrollBox.scrollHeight - (previousScroll + scrollBox.clientHeight) < 50;

    // Clear existing content
    while (container.firstChild) {
        container.removeChild(container.firstChild);
    }

    list.forEach((entry, index) => {
        const wrapper = document.createElement("div");
        wrapper.classList.add("sched-wrapper");

        // Ensure defaults
        entry.mode = entry.mode || "daily";
        entry.interval = entry.interval || 1;
        entry.time = entry.time || "00:15";
        entry.destinations = entry.destinations || [];
        // NEW: default queryType if missing
        if (entry.queryType === undefined || entry.queryType === null) {
            entry.queryType = 0; // Standard
        }

        let line1 = `
            <div class="sched-row">
                <select class="sched-mode" data-index="${index}">
                    <option value="minutes" ${entry.mode === "minutes" ? "selected" : ""}>Minutes</option>
                    <option value="hourly" ${entry.mode === "hourly" ? "selected" : ""}>Hourly</option>
                    <option value="daily" ${entry.mode === "daily" ? "selected" : ""}>Daily</option>
                    <option value="weekly" ${entry.mode === "weekly" ? "selected" : ""}>Weekly</option>
                    <option value="monthly" ${entry.mode === "monthly" ? "selected" : ""}>Monthly</option>
                </select>

                Every <input type="number" min="1" class="sched-interval" data-index="${index}" value="${entry.interval}" style="width:60px;">
        `;

        if (entry.mode === "minutes") line1 += ` minutes `;
        if (entry.mode === "hourly") line1 += ` hours `;
        if (entry.mode === "daily") line1 += ` days at <input type="time" class="sched-time" data-index="${index}" value="${entry.time}"> `;
        if (entry.mode === "weekly") {
            line1 += `
                weeks on 
                <select class="sched-day" data-index="${index}">
                    ${["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]
                    .map(d => `<option value="${d}" ${entry.day === d ? "selected" : ""}>${d}</option>`)
                    .join("")}
                </select>
                at <input type="time" class="sched-time" data-index="${index}" value="${entry.time}">
            `;
        }
        if (entry.mode === "monthly") {
            line1 += `
                months on day 
                <select class="sched-dayofmonth" data-index="${index}">
                    ${Array.from({ length: 31 }, (_, i) => i + 1)
                    .map(d => `<option value="${d}" ${entry.dayOfMonth === d ? "selected" : ""}>${d}</option>`)
                    .join("")}
                </select>
                at <input type="time" class="sched-time" data-index="${index}" value="${entry.time}">
            `;
        }

        line1 += `
                <button class="remove-sched" data-index="${index}">X</button>
            </div>
        `;

        // Query type dropdown per schedule entry
        const queryTypeLine = `
            <div class="sched-query-row">
                <span>Query Type:</span>
                <select class="sched-query-type" data-index="${index}">
                    <option value="0" ${entry.queryType === 0 ? "selected" : ""}>Standard</option>
                    <option value="1" ${entry.queryType === 1 ? "selected" : ""}>Partner ID</option>
                    <option value="2" ${entry.queryType === 2 ? "selected" : ""}>Rotator Only</option>
                    <option value="3" ${entry.queryType === 3 ? "selected" : ""}>Rotator + MAID</option>
                    <option value="4" ${entry.queryType === 4 ? "selected" : ""}>Rotator + Partner ID</option>
                    <option value="5" ${entry.queryType === 5 ? "selected" : ""}>Rotator + MAID + Partner ID</option>
                </select>
            </div>
        `;

        let line2 = `
            <div class="sched-dest-block">
                <div class="sched-dest-label">Destinations:</div>
                <div id="sched-dest-list-${index}" class="sched-dest-list"></div>

                <div class="sched-actions">
                    <button id="sched-add-dest-${index}" class="sched-add-dest">Add Destination</button>
                    <button class="sched-execute-now" data-index="${index}">Execute Now</button>
                </div>
            </div>
        `;

        wrapper.innerHTML = line1 + queryTypeLine + line2;
        container.appendChild(wrapper);

        renderDestinationsForSchedule(entry, index);
    });

    // Existing listeners

    document.querySelectorAll(".sched-mode").forEach(sel => {
        sel.addEventListener("change", () => {
            const idx = sel.dataset.index;
            list[idx].mode = sel.value;
            appendLog(`[INFO] Schedule row ${idx} mode changed to ${sel.value}`);
            renderSchedule(list);
        });
    });

    document.querySelectorAll(".sched-interval").forEach(inp => {
        inp.addEventListener("input", () => {
            const idx = inp.dataset.index;
            list[idx].interval = parseInt(inp.value);
            appendLog(`[INFO] Schedule row ${idx} interval set to ${inp.value}`);
        });
    });

    document.querySelectorAll(".sched-time").forEach(inp => {
        inp.addEventListener("input", () => {
            const idx = inp.dataset.index;
            list[idx].time = inp.value;
            appendLog(`[INFO] Schedule row ${idx} time set to ${inp.value}`);
        });
    });

    document.querySelectorAll(".sched-day").forEach(sel => {
        sel.addEventListener("change", () => {
            const idx = sel.dataset.index;
            list[idx].day = sel.value;
            appendLog(`[INFO] Schedule row ${idx} day set to ${sel.value}`);
        });
    });

    document.querySelectorAll(".sched-dayofmonth").forEach(sel => {
        sel.addEventListener("change", () => {
            const idx = sel.dataset.index;
            list[idx].dayOfMonth = parseInt(sel.value);
            appendLog(`[INFO] Schedule row ${idx} day-of-month set to ${sel.value}`);
        });
    });

    document.querySelectorAll(".remove-sched").forEach(btn => {
        btn.addEventListener("click", () => {
            const idx = parseInt(btn.dataset.index);
            list.splice(idx, 1);
            appendLog(`[INFO] Removed schedule row ${idx}`);
            renderSchedule(list);
        });
    });

    document.getElementById("add-sched-btn").onclick = () => {
        // Deep copy default, including queryType
        list.push(JSON.parse(JSON.stringify(DEFAULT_SCHEDULE_ENTRY)));
        appendLog("[INFO] Added new schedule entry (Daily, interval 1, time 00:15, Standard query)");
        renderSchedule(list);
    };

    // NEW: bind query type radio buttons
    document.querySelectorAll(".sched-query-type").forEach(r => {
        r.addEventListener("change", () => {
            const idx = parseInt(r.dataset.index);
            const value = parseInt(r.value);
            list[idx].queryType = value;
            appendLog(`[INFO] Schedule row ${idx} query type set to ${value}`);
        });
    });

    // ⭐ Execute Now button
    document.querySelectorAll(".sched-execute-now").forEach(btn => {
        btn.addEventListener("click", async () => {
            const idx = parseInt(btn.dataset.index);
            const task = list[idx];

            appendLog(`[INFO] Executing scheduled task #${idx} now…`);

            // Build payload for /api/ondemand
            const payload = {
                station: window.currentStation.stationName,
                dates: [new Date().toISOString().split("T")[0]], // today's date
                destination: task.destinations[0] || "",
                queryType: mapQueryType(task.queryType)
            };

            try {
                const res = await fetch("/api/ondemand", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                });

                const data = await res.json();

                if (data.success) {
                    appendLog(`[SUCCESS] Scheduled task #${idx} executed. Files written: ${data.files.length}`);
                } else {
                    appendLog(`[ERROR] Scheduled task #${idx} failed.`);
                }
            } catch (err) {
                appendLog(`[ERROR] Execute Now failed: ${err}`);
            }
        });
    });

    // ⭐ Restore scroll position AFTER rendering
    if (isNearBottom) {
        scrollBox.scrollTop = scrollBox.scrollHeight; // auto-scroll to bottom
    } else {
        scrollBox.scrollTop = previousScroll; // preserve user position
    }
}

function showError(msg) {
    const el = document.getElementById("error-message");
    el.style.color = "red";
    el.innerText = msg;
}

function showSuccess(msg) {
    const el = document.getElementById("error-message");
    el.style.color = "lightgreen";
    el.innerText = msg;
}

document.getElementById("sys-save-btn").addEventListener("click", saveSystemConfig);

loadSystemConfig();
startLogStream();