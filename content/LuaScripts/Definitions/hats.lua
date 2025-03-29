---@meta


---@class animFrame
---@field value integer
---@field delay number?

---@class animation
---@field delay number?
---@field looping boolean
---@field frames animFrame[]
---@field name string

---@class (exact) hatSprite
---@field timeAccumulator number
---@field currentFrameId integer
---@field frozen boolean
---@field currentAnimName string?
---@field animations {[string]: animation}
---@field forceCurrentFrame integer? 
---@field PreviousFrameId integer readonly
---@field Finished boolean readonly
---@field FrameChanged boolean readonly
---@field AnimChanged boolean readonly
---@field CurrentFrame animFrame readonly
local hatSpriteClass = {}

---@return animation?
function hatSpriteClass:currentAnim()
end

---@param animation animation
---@overload fun(self: hatSprite, name: string, delay: number, looping: boolean, frames: animFrame[])
function hatSpriteClass:addAnim(animation)
end


---@param name string
---@param clearState clearState?
function hatSpriteClass:setAnim(name, clearState)
end

---@param name string
---@return boolean
function hatSpriteClass:hasAnim(name)
end

---@return animFrame?
function hatSpriteClass:nextFrame()
end

function hatSpriteClass:clearFrameState()
end

---@param gameTime gameTime
function hatSpriteClass:update(gameTime)
end

---@return hatSprite
function hatSprite()
end

---@class hatId
---@field id number
---@field gen number

---@class depthAnimHat
---@field position vec2
---@field angle number
---@field sprite hatSprite
---@field depth number
---@field fippedHorizontally boolean
---@field teamsBitmap teamsBitmap
---@field id hatId
local depthAnimHatClass = {}

function depthAnimHatClass:update()
end

function depthAnimHatClass:remove()
end

---@return boolean
function depthAnimHatClass:isAlive()
end

---@class vanillaHat
---@field teamsBitmap teamsBitmap
---@field sprite hatSprite
---@field id hatId
local vanillaHatClass = {}

---@return vec2
function vanillaHatClass:getPosition()
end

---@return boolean
function vanillaHatClass:isAlive()
end

function vanillaHatClass:remove()
end

---@param pos vec2
function vanillaHatClass:setPosition(pos)
end

---@return number
function vanillaHatClass:getAngle()
end

---@param value boolean
function vanillaHatClass:setStrappedOn(value)
end

---@return boolean
function vanillaHatClass:getStrappedOn()
end

---@param duck duck
function vanillaHatClass:equip(duck)
end

function vanillaHatClass:unequip()
end

---@param angle number
function vanillaHatClass:setAngle(angle)
end

---@return boolean
function vanillaHatClass:getFlippedHorizontally()
end

---@param value boolean
function vanillaHatClass:setFlippedHorizontally(value)
end

---@class depthHat
---@field position vec2
---@field angle number
---@field sprite hatSprite
---@field depth number
---@field fippedHorizontally boolean
---@field teamsBitmap teamsBitmap
---@field id hatId
local depthHatClass = {}

function depthHatClass:update()
end

function depthHatClass:remove()
end

---@return boolean
function depthHatClass:isAlive()
end

---@param state depthHatState
function depthHatClass:setState(state)
end

---@class teamGen
---@field value number

---@class teamId
---@field value number

---@class teamHandle
---@field gen teamGen
---@field id teamId

---@class teamFrame
---@field teamHandles teamHandle[]

---@class teamsBitmap
---@field isBig boolean
---@field frames teamFrame[]
---@field frameSize vec2

---@param path string
---@param frameSize vec2
---@return teamsBitmap?
function teamsBitmap(path, frameSize)
end

---@return animation
---@param delay number
---@param looping boolean
---@param name string
---@param frames animFrame[]
function animation(name, delay, looping, frames) end

---@param teamsBitmap teamsBitmap
---@return depthHat
function depthHat(teamsBitmap)
end

---@param teamsBitmap teamsBitmap
---@return depthAnimHat
function depthAnimHat(teamsBitmap)
end

---@param teamsBitmap teamsBitmap
---@return vanillaHat
function vanillaHat(teamsBitmap)
end