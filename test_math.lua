-- Math constants
print("=== Math Constants ===")
print("math.pi =", math.pi)
print("math.huge =", math.huge)
print("math.maxinteger =", math.maxinteger)
print("math.mininteger =", math.mininteger)

-- Basic arithmetic
print("\n=== Basic Arithmetic ===")
print("math.abs(-5) =", math.abs(-5))
print("math.max(1, 5, 3) =", math.max(1, 5, 3))
print("math.min(1, 5, 3) =", math.min(1, 5, 3))
print("math.floor(3.7) =", math.floor(3.7))
print("math.ceil(3.2) =", math.ceil(3.2))

-- Trigonometry
print("\n=== Trigonometry ===")
print("math.sin(math.pi/2) =", math.sin(math.pi/2))
print("math.cos(0) =", math.cos(0))
print("math.tan(math.pi/4) =", math.tan(math.pi/4))
print("math.deg(math.pi) =", math.deg(math.pi))
print("math.rad(180) =", math.rad(180))

-- Exponential/Logarithmic
print("\n=== Exponential/Logarithmic ===")
print("math.exp(1) =", math.exp(1))
print("math.log(math.exp(1)) =", math.log(math.exp(1)))
print("math.sqrt(16) =", math.sqrt(16))
print("math.pow(2, 3) =", math.pow(2, 3))

-- Random
print("\n=== Random Functions ===")
math.randomseed(12345)
print("math.random() =", math.random())
print("math.random(10) =", math.random(10))
print("math.random(5, 15) =", math.random(5, 15))

-- Type functions
print("\n=== Type Functions ===")
print("math.type(42) =", math.type(42))
print("math.type(3.14) =", math.type(3.14))
print("math.tointeger(3.14) =", math.tointeger(3.14))
print("math.tointeger(3.0) =", math.tointeger(3.0))
