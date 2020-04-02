
function [Output] = Normalization(Data,size)

A(:,1)=Data;
A(:,2)=Data;
B=resizem(A, [size,2],'bilinear');

Output=B(:,1);

end