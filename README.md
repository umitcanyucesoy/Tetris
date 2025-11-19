# Tetris

A comprehensive Tetris game implementation featuring:

- 🎲 **Round Robin piece randomizer**
- 🔄 **Super Rotation System (SRS)**
- 🔷 **T-Spin & T-Spin Double detection**

---

## 🎲 Round Robin Piece Randomizer

<img width="512" height="512" alt="ChatGPT Image Nov 20, 2025, 12_18_38 AM" src="https://github.com/user-attachments/assets/ad444d47-9722-4ded-ae32-a016a0ea55a9" />


This project uses a **Round Robin–style randomizer** instead of a pure RNG.

- Each Tetromino has an internal **weight**.
- When a piece is **not** selected, its weight **increases** over time.
- When a piece **is** selected and spawned, its weight is **reset**.
- The next piece is chosen based on these weights, so pieces that haven’t appeared recently are more likely to be selected.

This keeps the game fair and reduces situations where:
- The same piece appears too frequently in a row, or  
- A specific piece (like the I piece) does not appear for a long time.

---

## 🔄 Super Rotation System (SRS)

<img width="551" height="322" alt="Screenshot 2025-11-20 at 00 28 09" src="https://github.com/user-attachments/assets/1e6c0444-706c-404b-bc31-5a68951cae15" />


The game implements the modern **Super Rotation System (SRS)** used in official Tetris titles.

Key points:

- Each piece rotates around a defined **pivot point**.
- When a rotation would cause a collision with a wall, the floor, or other blocks, the system uses a **kick table**:
  - A sequence of offset attempts (kicks) is tested.
  - The first valid position that does not collide is accepted.
- This allows familiar behaviors such as **wall kicks**, enabling the player to rotate pieces into tight spaces that would otherwise be unreachable.

Using SRS makes the rotation behavior consistent with modern Tetris expectations and is essential for advanced techniques like T-Spins.

---

## 🔷 T-Spin & T-Spin Double Detection

<img width="418" height="443" alt="Screenshot 2025-11-20 at 00 17 37" src="https://github.com/user-attachments/assets/ba020336-534b-4f65-9e5d-7511d5c9fe2a" />


The project includes logic to detect **T-Spin**, **Mini T-Spin**, and **T-Spin Double** moves.

General T-Spin rules used:

- The last player action before locking the piece is a **rotation** of the T piece.
- After the rotation, at least **3 of the 4 corner cells** around the T piece’s center are occupied (by walls or blocks).
- If this condition is met when the piece locks, the move is counted as a T-Spin (or Mini T-Spin depending on the shape’s final orientation and used kicks).

T-Spin Double specifics:

- A valid T-Spin is detected **and**
- The move clears **exactly 2 lines**.

These moves are scored separately from normal line clears, rewarding players for using advanced rotational techniques and tight placements.
