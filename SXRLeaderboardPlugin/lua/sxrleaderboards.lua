--[[
    SXR Leaderboards - In-Game Leaderboard Display
    
    Features:
    - Player Ranks: Overall competitive standings
    - Time Trials: Fastest times by route
    - Longest Run: Endurance racing (races before pitting)
    
    Accessible via Extended Chat / Online Extras menu (TAB key)
]]

-- API URL
local baseUrl = "http://" .. ac.getServerIP() .. ":" .. ac.getServerPortHTTP() .. "/sxrleaderboards"
local steamId = ac.getUserSteamID()

-- State
local state = {
    enabled = true,
    currentTab = 1,  -- 1=PlayerRanks, 2=TimeTrials, 3=LongestRun
    page = 1,
    pageSize = 15,
    totalPages = 1,
    loading = false,
    lastRefresh = 0,
    refreshInterval = 30,
    
    -- Data
    playerRanks = {},
    timeTrials = {},
    longestRuns = {},
    routes = {},
    selectedRoute = "",  -- Filter by specific route
    
    -- Active run display
    activeRun = nil,
    showActiveRun = true,
    
    -- Player's own data
    myRank = nil,
    myBestTime = nil,
    myBestRun = nil
}

local tabNames = { "Player Ranks", "Time Trials", "Longest Run" }

local colors = {
    gold = rgbm(1, 0.84, 0, 1),
    silver = rgbm(0.75, 0.75, 0.75, 1),
    bronze = rgbm(0.8, 0.5, 0.2, 1),
    accent = rgbm(0, 0.8, 1, 1),
    dimmed = rgbm(0.6, 0.6, 0.6, 1),
    green = rgbm(0.2, 0.8, 0.2, 1),
    red = rgbm(0.9, 0.2, 0.2, 1),
    yellow = rgbm(1, 1, 0, 1),
    bg = rgbm(0.08, 0.08, 0.1, 0.95),
    bgLight = rgbm(0.12, 0.12, 0.15, 1),
    bgHeader = rgbm(0.15, 0.15, 0.2, 1),
    
    -- Prestige colors
    prestige = {
        [0] = rgbm(1, 1, 1, 1),           -- White
        [1] = rgbm(1, 0.84, 0, 1),        -- Gold
        [2] = rgbm(1, 0.42, 0.42, 1),     -- Coral
        [3] = rgbm(0.6, 0.2, 0.8, 1),     -- Purple
        [4] = rgbm(0.2, 0.6, 0.86, 1),    -- Blue
        [5] = rgbm(0.18, 0.8, 0.44, 1),   -- Emerald
    }
}

-- ============================================================================
-- API FUNCTIONS
-- ============================================================================

function FetchPlayerRanks()
    state.loading = true
    local url = baseUrl .. "/ranks?page=" .. state.page .. "&pageSize=" .. state.pageSize
    
    web.get(url, function(err, response)
        state.loading = false
        if not err and response.body then
            local data = stringify.parse(response.body)
            if data then
                state.playerRanks = data.Entries or {}
                state.totalPages = data.TotalPages or 1
            end
        end
        state.lastRefresh = os.clock()
    end)
end

function FetchTimeTrials()
    state.loading = true
    local url = baseUrl .. "/timetrials?page=" .. state.page .. "&pageSize=" .. state.pageSize
    
    -- Apply route filter
    if state.selectedRoute ~= "" then
        url = url .. "&route=" .. state.selectedRoute
    end
    
    web.get(url, function(err, response)
        state.loading = false
        if not err and response.body then
            local data = stringify.parse(response.body)
            if data then
                state.timeTrials = data.Entries or {}
                state.totalPages = data.TotalPages or 1
            end
        end
        state.lastRefresh = os.clock()
    end)
end

function FetchLongestRuns()
    state.loading = true
    local url = baseUrl .. "/longestruns?page=" .. state.page .. "&pageSize=" .. state.pageSize
    
    web.get(url, function(err, response)
        state.loading = false
        if not err and response.body then
            local data = stringify.parse(response.body)
            if data then
                state.longestRuns = data.Entries or {}
                state.totalPages = data.TotalPages or 1
            end
        end
        state.lastRefresh = os.clock()
    end)
end

function FetchRoutes()
    web.get(baseUrl .. "/routes", function(err, response)
        if not err and response.body then
            state.routes = stringify.parse(response.body) or {}
        end
    end)
end

function FetchMyData()
    -- Get my rank
    web.get(baseUrl .. "/ranks/" .. steamId, function(err, response)
        if not err and response.body then
            state.myRank = stringify.parse(response.body)
        end
    end)
    
    -- Get my active run
    web.get(baseUrl .. "/activeruns/" .. steamId, function(err, response)
        if not err and response.body then
            state.activeRun = stringify.parse(response.body)
        else
            state.activeRun = nil
        end
    end)
end

function RefreshCurrentTab()
    if state.currentTab == 1 then
        FetchPlayerRanks()
    elseif state.currentTab == 2 then
        FetchTimeTrials()
    else
        FetchLongestRuns()
    end
    FetchMyData()
end

-- ============================================================================
-- UI HELPERS
-- ============================================================================

function GetRankColor(rank)
    if rank == 1 then return colors.gold
    elseif rank == 2 then return colors.silver
    elseif rank == 3 then return colors.bronze
    else return colors.dimmed
    end
end

function GetPrestigeColor(prestige)
    if prestige >= 50 then
        -- Rainbow for P50+
        local hue = (os.clock() * 0.5) % 1.0
        local i = math.floor(hue * 6)
        local f = hue * 6 - i
        local q = 1 - f
        i = i % 6
        if i == 0 then return rgbm(1, f, 0, 1)
        elseif i == 1 then return rgbm(q, 1, 0, 1)
        elseif i == 2 then return rgbm(0, 1, f, 1)
        elseif i == 3 then return rgbm(0, q, 1, 1)
        elseif i == 4 then return rgbm(f, 0, 1, 1)
        else return rgbm(1, 0, q, 1) end
    elseif prestige >= 20 then return rgbm(0, 1, 1, 1)
    elseif prestige >= 10 then return rgbm(1, 0, 1, 1)
    elseif colors.prestige[prestige] then return colors.prestige[prestige]
    else return colors.prestige[0]
    end
end

function FormatDriverLevel(level, prestige)
    if prestige > 0 then
        return string.format("P%d-%d", prestige, level)
    else
        return tostring(level)
    end
end

function FormatTime(ms)
    local secs = ms / 1000
    local mins = math.floor(secs / 60)
    local remainSecs = secs % 60
    return string.format("%d:%06.3f", mins, remainSecs)
end

function FormatDuration(seconds)
    local hours = math.floor(seconds / 3600)
    local mins = math.floor((seconds % 3600) / 60)
    local secs = math.floor(seconds % 60)
    if hours > 0 then
        return string.format("%d:%02d:%02d", hours, mins, secs)
    else
        return string.format("%d:%02d", mins, secs)
    end
end

-- ============================================================================
-- TAB CONTENT
-- ============================================================================

function DrawPlayerRanksTab()
    -- Header
    ui.pushFont(ui.Font.Small)
    ui.columns(7, 'rankCols')
    ui.setColumnWidth(0, 40)   -- Rank
    ui.setColumnWidth(1, 100)  -- Level
    ui.setColumnWidth(2, 120)  -- Name
    ui.setColumnWidth(3, 100)  -- Favorite Car
    ui.setColumnWidth(4, 70)   -- W/L
    ui.setColumnWidth(5, 60)   -- Club
    ui.setColumnWidth(6, 70)   -- Avg Speed
    
    ui.textColored("#", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Level", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Player", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Favorite Car", colors.dimmed)
    ui.nextColumn()
    ui.textColored("W/Races", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Club", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Avg Spd", colors.dimmed)
    ui.columns(1)
    ui.popFont()
    
    ui.separator()
    
    -- Entries
    for i, entry in ipairs(state.playerRanks) do
        ui.pushID(i)
        local isMe = entry.SteamId == steamId
        
        ui.columns(7, 'rankCols')
        
        -- Rank
        ui.textColored(tostring(entry.Rank), GetRankColor(entry.Rank))
        ui.nextColumn()
        
        -- Level with prestige
        local levelText = FormatDriverLevel(entry.DriverLevel, entry.PrestigeRank)
        ui.textColored(levelText, GetPrestigeColor(entry.PrestigeRank))
        ui.nextColumn()
        
        -- Name (highlight self)
        if isMe then
            ui.textColored(entry.PlayerName:sub(1, 15), colors.accent)
        else
            ui.text(entry.PlayerName:sub(1, 15))
        end
        ui.nextColumn()
        
        -- Favorite car
        ui.text((entry.FavoriteCarDisplayName or entry.FavoriteCar or ""):sub(1, 12))
        ui.nextColumn()
        
        -- Wins/Races
        ui.text(entry.WinLossDisplay or "0/0")
        ui.nextColumn()
        
        -- Club tag
        ui.textColored(entry.ClubTag or "---", colors.dimmed)
        ui.nextColumn()
        
        -- Avg speed
        ui.text(entry.AvgSpeedDisplay or "0 km/h")
        
        ui.columns(1)
        ui.popID()
    end
end

function DrawTimeTrialsTab()
    -- Route selector
    ui.text("Route: ")
    ui.sameLine()
    ui.setNextItemWidth(180)
    local routeLabel = state.selectedRoute == "" and "All Routes" or GetRouteDisplayName(state.selectedRoute)
    if ui.beginCombo("##routeSelect", routeLabel) then
        if ui.selectable("All Routes", state.selectedRoute == "") then
            state.selectedRoute = ""
            state.page = 1
            FetchTimeTrials()
        end
        ui.separator()
        for _, route in ipairs(state.routes) do
            local label = route.RouteName
            if route.DistanceKm and route.DistanceKm > 0 then
                label = label .. " (" .. string.format("%.1f", route.DistanceKm) .. " km)"
            end
            if ui.selectable(label, state.selectedRoute == route.RouteId) then
                state.selectedRoute = route.RouteId
                state.page = 1
                FetchTimeTrials()
            end
        end
        ui.endCombo()
    end
    
    ui.separator()
    
    -- Info about clean/dirty ranking
    ui.pushFont(ui.Font.Small)
    ui.textColored("Clean times within 2 min of dirty times rank higher", colors.dimmed)
    ui.popFont()
    
    ui.separator()
    
    -- Header
    ui.pushFont(ui.Font.Small)
    ui.columns(6, 'ttCols')
    ui.setColumnWidth(0, 40)   -- Rank
    ui.setColumnWidth(1, 120)  -- Player
    ui.setColumnWidth(2, 100)  -- Car
    ui.setColumnWidth(3, 100)  -- Route
    ui.setColumnWidth(4, 80)   -- Time
    ui.setColumnWidth(5, 60)   -- Clean/Dirty
    
    ui.textColored("#", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Player", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Car", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Route", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Time", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Status", colors.dimmed)
    ui.columns(1)
    ui.popFont()
    
    ui.separator()
    
    -- Entries
    for i, entry in ipairs(state.timeTrials) do
        ui.pushID(i)
        local isMe = entry.SteamId == steamId
        
        ui.columns(6, 'ttCols')
        
        -- Rank
        ui.textColored(tostring(entry.LeaderboardRank), GetRankColor(entry.LeaderboardRank))
        ui.nextColumn()
        
        -- Player
        if isMe then
            ui.textColored(entry.PlayerName:sub(1, 15), colors.accent)
        else
            ui.text(entry.PlayerName:sub(1, 15))
        end
        ui.nextColumn()
        
        -- Car
        ui.text((entry.CarDisplayName or entry.CarModel or ""):sub(1, 12))
        ui.nextColumn()
        
        -- Route
        ui.text((entry.RouteName or ""):sub(1, 12))
        ui.nextColumn()
        
        -- Time
        ui.textColored(entry.TimeDisplay or "0:00.000", colors.gold)
        ui.nextColumn()
        
        -- Clean/Dirty
        if entry.IsDirty then
            ui.textColored("Dirty", colors.red)
        else
            ui.textColored("Clean", colors.green)
        end
        
        ui.columns(1)
        ui.popID()
    end
end

-- Helper to get route display name from ID
function GetRouteDisplayName(routeId)
    for _, route in ipairs(state.routes) do
        if route.RouteId == routeId then
            return route.RouteName
        end
    end
    return routeId
end

function DrawLongestRunTab()
    -- Show active run if exists
    if state.activeRun and state.showActiveRun then
        ui.pushStyleColor(ui.StyleColor.ChildBg, colors.bgLight)
        ui.childWindow('activeRun', vec2(0, 50), true, ui.WindowFlags.None, function()
            ui.textColored("ACTIVE RUN", colors.yellow)
            ui.sameLine(100)
            ui.text("Races: ")
            ui.sameLine()
            ui.textColored(tostring(state.activeRun.RaceCount), colors.accent)
            ui.sameLine(200)
            ui.text("Wins: " .. state.activeRun.WinsInRun)
            ui.sameLine(280)
            ui.text("Distance: " .. string.format("%.1f km", state.activeRun.TotalDistanceKm))
        end)
        ui.popStyleColor()
        ui.spacing()
    end
    
    -- Header
    ui.pushFont(ui.Font.Small)
    ui.columns(6, 'lrCols')
    ui.setColumnWidth(0, 40)   -- Rank
    ui.setColumnWidth(1, 120)  -- Player
    ui.setColumnWidth(2, 70)   -- Races
    ui.setColumnWidth(3, 70)   -- W/L
    ui.setColumnWidth(4, 80)   -- Distance
    ui.setColumnWidth(5, 80)   -- Duration
    
    ui.textColored("#", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Player", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Races", colors.dimmed)
    ui.nextColumn()
    ui.textColored("W/L", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Distance", colors.dimmed)
    ui.nextColumn()
    ui.textColored("Duration", colors.dimmed)
    ui.columns(1)
    ui.popFont()
    
    ui.separator()
    
    -- Entries
    for i, entry in ipairs(state.longestRuns) do
        ui.pushID(i)
        local isMe = entry.SteamId == steamId
        
        ui.columns(6, 'lrCols')
        
        -- Rank
        ui.textColored(tostring(entry.LeaderboardRank), GetRankColor(entry.LeaderboardRank))
        ui.nextColumn()
        
        -- Player
        if isMe then
            ui.textColored(entry.PlayerName:sub(1, 15), colors.accent)
        else
            ui.text(entry.PlayerName:sub(1, 15))
        end
        ui.nextColumn()
        
        -- Race count
        ui.textColored(tostring(entry.RaceCount), colors.gold)
        ui.nextColumn()
        
        -- Wins/Losses
        ui.text(entry.WinsInRun .. "/" .. entry.LossesInRun)
        ui.nextColumn()
        
        -- Distance
        ui.text(entry.DistanceDisplay or "0 km")
        ui.nextColumn()
        
        -- Duration
        ui.text(entry.DurationDisplay or "0:00")
        
        ui.columns(1)
        ui.popID()
    end
end

-- ============================================================================
-- MAIN PANEL
-- ============================================================================

function DrawLeaderboardPanel()
    -- Auto-refresh
    if os.clock() - state.lastRefresh > state.refreshInterval then
        RefreshCurrentTab()
    end
    
    -- Header
    ui.pushFont(ui.Font.Title)
    ui.textColored("SXR LEADERBOARDS", colors.accent)
    ui.popFont()
    
    -- My stats summary
    if state.myRank then
        ui.sameLine(ui.availableSpaceX() - 200)
        ui.textColored("#" .. state.myRank.Rank, GetRankColor(state.myRank.Rank))
        ui.sameLine()
        ui.text("Elo: " .. state.myRank.EloRating)
        ui.sameLine()
        ui.text(state.myRank.WinLossDisplay)
    end
    
    ui.separator()
    
    -- Tab bar
    for i, name in ipairs(tabNames) do
        if i > 1 then ui.sameLine() end
        
        local isActive = state.currentTab == i
        if isActive then
            ui.pushStyleColor(ui.StyleColor.Button, colors.accent)
        end
        
        if ui.button(name, vec2(100, 25)) then
            if state.currentTab ~= i then
                state.currentTab = i
                state.page = 1
                RefreshCurrentTab()
            end
        end
        
        if isActive then
            ui.popStyleColor()
        end
    end
    
    ui.separator()
    
    -- Content area
    ui.childWindow('leaderboardContent', vec2(0, 280), true, ui.WindowFlags.None, function()
        if state.loading then
            ui.text("Loading...")
        elseif state.currentTab == 1 then
            DrawPlayerRanksTab()
        elseif state.currentTab == 2 then
            DrawTimeTrialsTab()
        else
            DrawLongestRunTab()
        end
    end)
    
    -- Pagination
    ui.separator()
    
    if state.page > 1 then
        if ui.button("< Prev", vec2(60, 0)) then
            state.page = state.page - 1
            RefreshCurrentTab()
        end
        ui.sameLine()
    end
    
    ui.text(string.format("Page %d / %d", state.page, state.totalPages))
    
    if state.page < state.totalPages then
        ui.sameLine()
        if ui.button("Next >", vec2(60, 0)) then
            state.page = state.page + 1
            RefreshCurrentTab()
        end
    end
    
    ui.sameLine(ui.availableSpaceX() - 60)
    if ui.button("Refresh", vec2(55, 0)) then
        RefreshCurrentTab()
    end
    
    return false  -- Keep panel open
end

-- ============================================================================
-- INITIALIZATION
-- ============================================================================

-- Initial data fetch
FetchRoutes()
FetchPlayerRanks()
FetchMyData()

-- Register in Online Extras menu
setTimeout(function()
    ui.registerOnlineExtra(
        ui.Icons.Trophy,
        "SXR Leaderboards",
        function() return true end,  -- Always visible
        DrawLeaderboardPanel,
        nil  -- No dispose needed
    )
end, 1000)
