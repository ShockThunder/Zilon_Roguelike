﻿Feature: Monster_NotThrowWhenKill
	Чтобы монстры не поддерживали развитие
	Как разработчкиу
	Мне нужно, чтобы при убийстве игрока монстром не вываливалось исключение.

@monsters @dev0 @perks
Scenario: Для монстров не поддерживается развитие. Для них не должно выбрасываться исключение при попытке прокачать перки.
	Given Есть карта размером 2
	And Есть актёр игрока класса captain в ячейке (0, 0)
	And Есть монстр класса rat Id:100 в ячейке (1, 0)
	And Актёр игрока имеет Hp: 1
	And Монстр Id:100 имеет Hp 1000
	When Актёр игрока атакует монстра Id:100
	Then Актёр игрока мертв
