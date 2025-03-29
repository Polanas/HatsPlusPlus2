---@meta
---@class ducks
---@field main duck
---@field all duck[]
ducks = {}

---@class profile
---@field name string

---@class ragdoll
---@field part1 ragdollPart
---@field part2 ragdollPart
---@field part3 ragdollPart
---@field position vec2
---@field angle number
---@field angleDegrees number
---@field depth number

---@class ragdollPart
---@field position vec2
---@field angle number
---@field angleDegrees number
---@field depth number

---@class duck
---@field offdir integer
---@field reflect reflect
---@field position vec2
---@field jumping boolean
---@field immobilized boolean
---@field swinging boolean
---@field angle number
---@field angleDegrees number
---@field dead number
---@field depth number
---@field spriteFrame number
---@field velocity vec2
---@field crouch boolean
---@field profile profile
---@field trapped boolean
---@field ragdoll ragdoll?
---@field sliding boolean