
local utils = require('utils')
local M = {}

function M.distance(x1, y1, x2, y2)
    local dx = x2 - x1
    local dy = y2 - y1
    return math.sqrt(utils.add(dx * dx, dy * dy))
end

function M.average(numbers)
    local sum = 0
    for _, n in ipairs(numbers) do
        sum = utils.add(sum, n)
    end
    return sum / #numbers
end

M.factorial = utils.factorial  -- Re-export

return M
