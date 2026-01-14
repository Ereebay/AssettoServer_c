--[[
    SXR Route Editor - Admin Tool for Creating Time Trial Routes
    
    Pop-out window for admins to create and manage time trial routes.
    
    Features:
    - Create new routes with start/finish zones
    - Add checkpoints along the route
    - Capture current position for zones
    - Edit existing routes
    - Delete routes
    - Preview route on map
    
    Access: Press F9 to toggle (admins only)
]]

-- API URL
local baseUrl = "http://" .. ac.getServerIP() .. ":" .. ac.getServerPortHTTP() .. "/sxrleaderboards"
local adminUrl = "http://" .. ac.getServerIP() .. ":" .. ac.getServerPortHTTP() .. "/admin"
local steamId = ac.getUserSteamID()

-- Editor state
local editor = {
    visible = false,
    isAdmin = false,
    mode = "list",  -- list, create, edit
    
    -- Routes data
    routes = {},
    selectedRouteIndex = 0,
    
    -- New/Edit route data
    editRoute = {
        RouteId = "",
        RouteName = "",
        Category = "",
        DistanceKm = 0,
        Description = "",
        IsActive = true,
        StartZone = { X = 0, Y = 0, Z = 0, Radius = 30 },
        FinishZone = { X = 0, Y = 0, Z = 0, Radius = 30 },
        Checkpoints = {}
    },
    
    -- UI state
    showConfirmDelete = false,
    deleteTargetId = "",
    statusMessage = "",
    statusTime = 0,
    
    -- Capture mode
    captureMode = nil,  -- "start", "finish", "checkpoint"
    captureCheckpointIndex = 0
}

local colors = {
    accent = rgbm(0, 0.8, 1, 1),
    gold = rgbm(1, 0.84, 0, 1),
    green = rgbm(0.2, 0.8, 0.2, 1),
    red = rgbm(0.9, 0.2, 0.2, 1),
    yellow = rgbm(1, 1, 0, 1),
    dimmed = rgbm(0.6, 0.6, 0.6, 1),
    bg = rgbm(0.08, 0.08, 0.1, 0.98),
    bgLight = rgbm(0.12, 0.12, 0.15, 1),
    bgHeader = rgbm(0.15, 0.15, 0.2, 1)
}

-- ============================================================================
-- API FUNCTIONS
-- ============================================================================

function CheckAdminStatus()
    web.get(adminUrl .. "/status", function(err, response)
        if err then
            editor.isAdmin = false
            return
        end
        
        local data = stringify.parse(response.body)
        if data and data.ConnectedAdmins then
            for _, admin in ipairs(data.ConnectedAdmins) do
                if admin.SteamId == steamId then
                    editor.isAdmin = true
                    return
                end
            end
        end
        editor.isAdmin = false
    end)
end

function FetchRoutes()
    web.get(baseUrl .. "/routes", function(err, response)
        if not err and response.body then
            editor.routes = stringify.parse(response.body) or {}
        end
    end)
end

function SaveRoute(route, isNew)
    local endpoint = isNew and "/routes" or "/routes/" .. route.RouteId
    local method = isNew and "POST" or "PUT"
    
    local body = stringify(route)
    
    web.request(baseUrl .. endpoint .. "?adminSteamId=" .. steamId, {
        method = method,
        body = body,
        headers = { ["Content-Type"] = "application/json" }
    }, function(err, response)
        if not err then
            local result = stringify.parse(response.body)
            if result and result.Success then
                SetStatus("Route saved successfully!", colors.green)
                FetchRoutes()
                editor.mode = "list"
            else
                SetStatus("Failed to save: " .. (result and result.Message or "Unknown error"), colors.red)
            end
        else
            SetStatus("Error saving route", colors.red)
        end
    end)
end

function DeleteRoute(routeId)
    web.request(baseUrl .. "/routes/" .. routeId .. "?adminSteamId=" .. steamId, {
        method = "DELETE"
    }, function(err, response)
        if not err then
            SetStatus("Route deleted", colors.green)
            FetchRoutes()
        else
            SetStatus("Error deleting route", colors.red)
        end
    end)
end

function SetStatus(message, color)
    editor.statusMessage = message
    editor.statusTime = os.clock()
end

-- ============================================================================
-- POSITION CAPTURE
-- ============================================================================

function GetCurrentPosition()
    local car = ac.getCar(0)
    if car then
        return {
            X = car.position.x,
            Y = car.position.y,
            Z = car.position.z
        }
    end
    return { X = 0, Y = 0, Z = 0 }
end

function CapturePosition(target)
    local pos = GetCurrentPosition()
    
    if target == "start" then
        editor.editRoute.StartZone.X = pos.X
        editor.editRoute.StartZone.Y = pos.Y
        editor.editRoute.StartZone.Z = pos.Z
        SetStatus("Start zone position captured!", colors.green)
    elseif target == "finish" then
        editor.editRoute.FinishZone.X = pos.X
        editor.editRoute.FinishZone.Y = pos.Y
        editor.editRoute.FinishZone.Z = pos.Z
        SetStatus("Finish zone position captured!", colors.green)
    elseif target == "checkpoint" then
        local cp = {
            Order = #editor.editRoute.Checkpoints + 1,
            X = pos.X,
            Y = pos.Y,
            Z = pos.Z,
            Radius = 50
        }
        table.insert(editor.editRoute.Checkpoints, cp)
        SetStatus("Checkpoint #" .. cp.Order .. " added!", colors.green)
    end
    
    editor.captureMode = nil
end

-- ============================================================================
-- UI DRAWING
-- ============================================================================

function DrawRouteList()
    ui.text("Time Trial Routes (" .. #editor.routes .. ")")
    ui.sameLine(ui.availableSpaceX() - 100)
    if ui.button("+ New Route", vec2(95, 0)) then
        editor.mode = "create"
        editor.editRoute = {
            RouteId = "",
            RouteName = "",
            Category = "",
            DistanceKm = 0,
            Description = "",
            IsActive = true,
            StartZone = { X = 0, Y = 0, Z = 0, Radius = 30 },
            FinishZone = { X = 0, Y = 0, Z = 0, Radius = 30 },
            Checkpoints = {}
        }
    end
    
    ui.separator()
    
    -- Route list
    ui.childWindow('routeList', vec2(0, 300), true, ui.WindowFlags.None, function()
        for i, route in ipairs(editor.routes) do
            ui.pushID(i)
            
            -- Status indicator
            if route.IsActive then
                ui.textColored("●", colors.green)
            else
                ui.textColored("○", colors.dimmed)
            end
            ui.sameLine(20)
            
            -- Route name
            if ui.selectable(route.RouteName, editor.selectedRouteIndex == i, ui.SelectableFlags.None, vec2(200, 0)) then
                editor.selectedRouteIndex = i
            end
            
            ui.sameLine(230)
            ui.textColored(string.format("%.1f km", route.DistanceKm or 0), colors.dimmed)
            
            ui.sameLine(ui.availableSpaceX() - 80)
            if ui.button("Edit##" .. i, vec2(35, 0)) then
                editor.mode = "edit"
                editor.editRoute = {
                    RouteId = route.RouteId,
                    RouteName = route.RouteName,
                    Category = route.Category or "",
                    DistanceKm = route.DistanceKm or 0,
                    Description = route.Description or "",
                    IsActive = route.IsActive ~= false,
                    StartZone = route.StartZone or { X = 0, Y = 0, Z = 0, Radius = 30 },
                    FinishZone = route.FinishZone or { X = 0, Y = 0, Z = 0, Radius = 30 },
                    Checkpoints = route.Checkpoints or {}
                }
            end
            
            ui.sameLine()
            ui.pushStyleColor(ui.StyleColor.Button, rgbm(0.5, 0.1, 0.1, 1))
            if ui.button("X##del" .. i, vec2(25, 0)) then
                editor.showConfirmDelete = true
                editor.deleteTargetId = route.RouteId
            end
            ui.popStyleColor()
            
            ui.popID()
        end
    end)
    
    -- Selected route preview
    if editor.selectedRouteIndex > 0 and editor.selectedRouteIndex <= #editor.routes then
        local route = editor.routes[editor.selectedRouteIndex]
        ui.separator()
        ui.textColored("Selected: " .. route.RouteName, colors.accent)
        if route.Description and route.Description ~= "" then
            ui.textWrapped(route.Description)
        end
        ui.text("Checkpoints: " .. (#(route.Checkpoints or {})))
    end
    
    -- Delete confirmation
    if editor.showConfirmDelete then
        ui.openPopup("Confirm Delete")
    end
    
    if ui.beginPopupModal("Confirm Delete", true, ui.WindowFlags.AlwaysAutoResize) then
        ui.text("Delete this route?")
        ui.text("This cannot be undone!")
        ui.spacing()
        
        if ui.button("Yes, Delete", vec2(100, 0)) then
            DeleteRoute(editor.deleteTargetId)
            editor.showConfirmDelete = false
            ui.closePopup()
        end
        ui.sameLine()
        if ui.button("Cancel", vec2(100, 0)) then
            editor.showConfirmDelete = false
            ui.closePopup()
        end
        
        ui.endPopup()
    end
end

function DrawRouteEditor()
    local isNew = editor.mode == "create"
    ui.text(isNew and "Create New Route" or "Edit Route")
    
    ui.sameLine(ui.availableSpaceX() - 60)
    if ui.button("< Back", vec2(55, 0)) then
        editor.mode = "list"
    end
    
    ui.separator()
    
    ui.childWindow('routeEditor', vec2(0, 320), true, ui.WindowFlags.None, function()
        -- Basic info
        ui.textColored("Basic Information", colors.accent)
        ui.spacing()
        
        -- Route ID (only editable for new routes)
        ui.text("Route ID:")
        ui.sameLine(100)
        ui.setNextItemWidth(200)
        if isNew then
            local changed, newVal = ui.inputText("##routeId", editor.editRoute.RouteId, ui.InputTextFlags.None)
            if changed then editor.editRoute.RouteId = newVal end
        else
            ui.textColored(editor.editRoute.RouteId, colors.dimmed)
        end
        
        -- Route Name
        ui.text("Name:")
        ui.sameLine(100)
        ui.setNextItemWidth(200)
        local changed, newVal = ui.inputText("##routeName", editor.editRoute.RouteName, ui.InputTextFlags.None)
        if changed then editor.editRoute.RouteName = newVal end
        
        -- Category
        ui.text("Category:")
        ui.sameLine(100)
        ui.setNextItemWidth(200)
        changed, newVal = ui.inputText("##category", editor.editRoute.Category, ui.InputTextFlags.None)
        if changed then editor.editRoute.Category = newVal end
        ui.sameLine()
        ui.textColored("(e.g., C1, Wangan, Sprint)", colors.dimmed)
        
        -- Distance
        ui.text("Distance:")
        ui.sameLine(100)
        ui.setNextItemWidth(100)
        editor.editRoute.DistanceKm = ui.slider("##distance", editor.editRoute.DistanceKm, 0, 50, "%.1f km")
        
        -- Description
        ui.text("Description:")
        ui.sameLine(100)
        ui.setNextItemWidth(250)
        changed, newVal = ui.inputText("##desc", editor.editRoute.Description, ui.InputTextFlags.None)
        if changed then editor.editRoute.Description = newVal end
        
        -- Active
        ui.text("Active:")
        ui.sameLine(100)
        changed, newVal = ui.checkbox("##active", editor.editRoute.IsActive)
        if changed then editor.editRoute.IsActive = newVal end
        
        ui.separator()
        
        -- Start Zone
        ui.textColored("Start Zone", colors.green)
        ui.spacing()
        
        ui.text("Position:")
        ui.sameLine(100)
        ui.text(string.format("X: %.1f  Y: %.1f  Z: %.1f", 
            editor.editRoute.StartZone.X, 
            editor.editRoute.StartZone.Y, 
            editor.editRoute.StartZone.Z))
        
        ui.text("Radius:")
        ui.sameLine(100)
        ui.setNextItemWidth(100)
        editor.editRoute.StartZone.Radius = ui.slider("##startRadius", editor.editRoute.StartZone.Radius, 10, 100, "%.0f m")
        
        ui.sameLine(220)
        if ui.button("Capture Start", vec2(100, 0)) then
            CapturePosition("start")
        end
        
        ui.separator()
        
        -- Finish Zone
        ui.textColored("Finish Zone", colors.gold)
        ui.spacing()
        
        ui.text("Position:")
        ui.sameLine(100)
        ui.text(string.format("X: %.1f  Y: %.1f  Z: %.1f", 
            editor.editRoute.FinishZone.X, 
            editor.editRoute.FinishZone.Y, 
            editor.editRoute.FinishZone.Z))
        
        ui.text("Radius:")
        ui.sameLine(100)
        ui.setNextItemWidth(100)
        editor.editRoute.FinishZone.Radius = ui.slider("##finishRadius", editor.editRoute.FinishZone.Radius, 10, 100, "%.0f m")
        
        ui.sameLine(220)
        if ui.button("Capture Finish", vec2(100, 0)) then
            CapturePosition("finish")
        end
        
        ui.sameLine()
        if ui.button("Same as Start", vec2(90, 0)) then
            editor.editRoute.FinishZone.X = editor.editRoute.StartZone.X
            editor.editRoute.FinishZone.Y = editor.editRoute.StartZone.Y
            editor.editRoute.FinishZone.Z = editor.editRoute.StartZone.Z
            SetStatus("Finish set to start position (loop)", colors.green)
        end
        
        ui.separator()
        
        -- Checkpoints
        ui.textColored("Checkpoints (" .. #editor.editRoute.Checkpoints .. ")", colors.accent)
        ui.sameLine(200)
        if ui.button("+ Add Checkpoint", vec2(110, 0)) then
            CapturePosition("checkpoint")
        end
        ui.spacing()
        
        if #editor.editRoute.Checkpoints > 0 then
            for i, cp in ipairs(editor.editRoute.Checkpoints) do
                ui.pushID("cp" .. i)
                ui.text(string.format("#%d: X:%.0f Y:%.0f Z:%.0f (R:%.0f)", 
                    cp.Order or i, cp.X, cp.Y, cp.Z, cp.Radius or 50))
                ui.sameLine(ui.availableSpaceX() - 25)
                if ui.button("X##cp", vec2(20, 0)) then
                    table.remove(editor.editRoute.Checkpoints, i)
                end
                ui.popID()
            end
        else
            ui.textColored("No checkpoints (optional for validation)", colors.dimmed)
        end
    end)
    
    ui.separator()
    
    -- Save button
    local canSave = editor.editRoute.RouteId ~= "" and editor.editRoute.RouteName ~= ""
    if not canSave then
        ui.pushStyleVar(ui.StyleVar.Alpha, 0.5)
    end
    if ui.button("Save Route", vec2(120, 30)) and canSave then
        SaveRoute(editor.editRoute, isNew)
    end
    if not canSave then
        ui.popStyleVar()
        ui.sameLine()
        ui.textColored("Route ID and Name required", colors.red)
    end
end

function DrawEditorWindow()
    if not editor.visible then return end
    if not editor.isAdmin then
        editor.visible = false
        return
    end
    
    ui.pushStyleColor(ui.StyleColor.WindowBg, colors.bg)
    ui.pushStyleColor(ui.StyleColor.TitleBg, colors.bgHeader)
    ui.pushStyleColor(ui.StyleColor.TitleBgActive, colors.bgHeader)
    
    ui.toolWindow("SXR Route Editor", vec2(400, 450), true, function()
        -- Header
        ui.pushFont(ui.Font.Title)
        ui.textColored("Route Editor", colors.gold)
        ui.popFont()
        ui.sameLine(ui.availableSpaceX() - 60)
        if ui.button("Refresh", vec2(55, 0)) then
            FetchRoutes()
        end
        
        ui.separator()
        
        -- Current position display
        local pos = GetCurrentPosition()
        ui.pushFont(ui.Font.Small)
        ui.textColored(string.format("Your Position: X:%.1f Y:%.1f Z:%.1f", pos.X, pos.Y, pos.Z), colors.dimmed)
        ui.popFont()
        
        ui.separator()
        
        -- Main content
        if editor.mode == "list" then
            DrawRouteList()
        else
            DrawRouteEditor()
        end
        
        -- Status message
        if editor.statusMessage ~= "" and os.clock() - editor.statusTime < 3 then
            ui.separator()
            ui.textColored(editor.statusMessage, colors.green)
        end
    end)
    
    ui.popStyleColor(3)
end

-- ============================================================================
-- HOTKEY & INITIALIZATION
-- ============================================================================

-- F9 to toggle editor
function script.update(dt)
    -- Check for F9 key
    if ac.isKeyPressed(ac.KeyIndex.F9) then
        if editor.isAdmin then
            editor.visible = not editor.visible
            if editor.visible then
                FetchRoutes()
            end
        end
    end
    
    -- Draw the editor window
    DrawEditorWindow()
end

-- Initial admin check
CheckAdminStatus()

-- Periodic admin check
setInterval(function()
    CheckAdminStatus()
end, 30000)
