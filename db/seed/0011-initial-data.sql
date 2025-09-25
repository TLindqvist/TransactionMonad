INSERT INTO OrderHead (Id, Supplier, Status)
VALUES (1, 'Fruits Inc', 'Draft');

INSERT INTO OrderLine (Id, OrderId, Item, Quantity_Amount, Quantity_UoM)
VALUES (1, 1, 'Banana', 6, 'Pcs');

INSERT INTO OrderLine (Id, OrderId, Item, Quantity_Amount, Quantity_UoM)
VALUES (2, 1, 'Apple', 1, 'Pcs');
